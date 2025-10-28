using fiskaltrust.Api.POS.Extensions;
using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Api.POS.v2.Echo;
using fiskaltrust.Api.POS.v2.Journal;
using fiskaltrust.Api.POS.v2.Sign;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;
using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.Api.PosSystemLocal.v2.Echo;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace fiskaltrust.Api.PosSystemLocal.OperationHandling;

public class OperationStateMachine
{
    private readonly ILogger<OperationStateMachine> _logger;
    private readonly IMiddlewareClient _middlewareClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOperationItemRepository _operationItemRepository;
    private readonly Guid _queueId;

    public OperationStateMachine(ILogger<OperationStateMachine> logger, IMiddlewareClient middlewareClient, Guid queueId, ILoggerFactory loggerFactory, IOperationItemRepository operationItemRepository)
    {
        _logger = logger;
        _middlewareClient = middlewareClient;
        _loggerFactory = loggerFactory;
        _operationItemRepository = operationItemRepository;
        _queueId = queueId;
    }

    public async Task<Result<TypedResults<ReceiptResponse>, ProblemDetails>> PerformSignAsync(PosSystemApiRequest request)
    {
        return await PerformOperationAsync(request, _queueId, _loggerFactory, new SignProcessor(_middlewareClient, _loggerFactory), new ReceiptRequestV2Validator(_loggerFactory.CreateLogger<ReceiptRequestV2Validator>()));
    }

    public async Task<Result<TypedResults<EchoResponse>, ProblemDetails>> PerformEchoAsync(PosSystemApiRequest request)
    {
        return await PerformOperationAsync(request, _queueId, _loggerFactory, new EchoProcessor(_loggerFactory.CreateLogger<EchoProcessor>(), _middlewareClient), new EchoRequestValidator(_loggerFactory.CreateLogger<EchoRequestValidator>()));
    }

    public async ValueTask<OperationItem?> State(Guid operationId) => await _operationItemRepository.GetAsync(operationId);

    public async Task<Result<TypedResults<TResponse>, ProblemDetails>> PerformOperationAsync<TRequest, TResponse>(PosSystemApiRequest request, Guid queueId, ILoggerFactory loggerFactory, IOperationProcessor<TRequest, TResponse> operationProcessor, IRequestValidator<TRequest> requestValidator)
    {
        var logger = loggerFactory.CreateLogger($"{request.Path}");
        var options = OperationProcessorOptions.FromContext(request);
        var (responseOperationItem, created) = await Request<TRequest, TResponse>(options, () => new OperationItem
        {
            cbOperationItemID = options.OperationID,
            ftQueueID = queueId,
            ftPosSystemID = options.PosSystemID,
            cbTerminalID = options.TerminalID,
            Method = request.Method,
            Path = request.Path,
            RequestHeaders = request.Headers.Filter(x => !new string[] { "x-cashbox-id", "x-cashbox-accesstoken", "cashboxid", "accesstoken" }.Contains(x.Key.ToLower())),
            Request = request.Body,
            TimeStamp = DateTime.UtcNow,
            ftOperationItemMoment = DateTime.UtcNow,
            LastState = OperationState.PENDING,
        }, operationProcessor, requestValidator);

        if (!created && request.Body != null && responseOperationItem.Request != request.Body)
        {
            return TypedResults<TResponse>.Problem(statusCode: 409, detail: "The operation body does not match the stored operation.");
        }

        if (responseOperationItem is null)
        {
            return TypedResults<TResponse>.NotFound();
        }

        if (responseOperationItem.LastState == OperationState.FAILED)
        {
            return TypedResults<TResponse>.Failed(statusCode: responseOperationItem.ResponseCode.Value, data: responseOperationItem.Response, location: $"/v2/journal/OperationItem/{options.OperationID}");
        }

        if (responseOperationItem.LastState == OperationState.DONE)
        {
            return TypedResults<TResponse>.Done(data: JsonSerializer.Deserialize<TResponse>(responseOperationItem.Response), statusCode: created ? 201 : 200, location: $"/v2/journal/OperationItem/{options.OperationID}");
        }

        return TypedResults<TResponse>.Problem(statusCode: 422, detail: "The operation is still in process. Please try again. Something went deeply wrong.");
    }

    public async ValueTask<(OperationItem operationItem, bool created)> Request<TRequest, TResponse>(OperationProcessorOptions options, Func<OperationItem> constructor, IOperationProcessor<TRequest, TResponse> operationProcessor, IRequestValidator<TRequest> requestValidator)
    {
        //await using var creatingGate = await _operationGateFactory.AcquireCreatingGateAsync(options.OperationID);
        var operationItem = await _operationItemRepository.GetAsync(options.OperationID);

        if (operationItem is not null)
        {
            // retry operation if it's in pending state?
            return (operationItem, false);
        }

        operationItem = constructor();

        // var processingGate = await _operationGateFactory.AcquireProcessingGateAsync(operationItem.cbOperationItemID);
        try
        {
            await _operationItemRepository.InsertAsync(operationItem);
        }
        catch
        {
            // await processingGate.DisposeAsync();
            throw;
        }

        var result = operationItem.Clone() as OperationItem;
        // await using var _ = processingGate;

        operationItem.LastState = OperationState.PROCESSING;
        await _operationItemRepository.InsertAsync(operationItem);
        var response = await requestValidator
            .Validate(operationItem.Request)
            .AndThenAsync(async request =>
            {
                Result<TResponse, ProblemDetails> result;
                try
                {
                    result = await operationProcessor.ProcessAsync(request, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while processing operation");
                    result = TypedResults<TResponse>.Problem(statusCode: 500, detail: ex.Message);
                }
                return result.MapErr<ValidationError>(err => err);
            });


        response.Match(
            ok =>
            {
                operationItem.Response = JsonSerializer.Serialize(ok);
                operationItem.LastState = OperationState.DONE;
                operationItem.ResponseCode = 201;
            },
            err =>
            {
                operationItem.LastState = OperationState.FAILED;
                (operationItem.ResponseCode, operationItem.Response) = err switch
                {
                    ValidationError.Problem problem => (problem.Error.Status, JsonSerializer.Serialize(problem.Error.Detail)),
                };
            }
        );

        operationItem.TimeStamp = DateTime.UtcNow;
        await _operationItemRepository.InsertAsync(operationItem);
        return (operationItem, true);
    }
}
