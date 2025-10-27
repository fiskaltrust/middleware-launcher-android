using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.ifPOS.v2;
using Microsoft.Extensions.Logging;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;
using fiskaltrust.Api.PosSystemLocal.OperationHandling;


namespace fiskaltrust.Api.PosSystemLocal.v2.Echo;

public class EchoProcessor(
    ILogger<EchoProcessor> logger,
    IMiddlewareClient middlewareClient
    ) : IOperationProcessor<EchoRequest, EchoResponse>
{
    public async Task<Result<EchoResponse, ProblemDetails>> ProcessAsync(EchoRequest request, OperationProcessorOptions options)
    {
        var response = await middlewareClient.EchoV2Async(new EchoRequest
        {
            Message = request.Message
        });

        if (response.error != null)
        {
            return TypedResults<EchoResponse>.Problem(statusCode: 500, detail: response.error);
        }

        return new EchoResponse
        {
            Message = response.Item1.Message
        };
    }
}