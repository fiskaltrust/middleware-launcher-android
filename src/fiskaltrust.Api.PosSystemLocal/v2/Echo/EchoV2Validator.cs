using fiskaltrust.Api.POS.Helpers;
using FluentValidation;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;

namespace fiskaltrust.Api.POS.v2.Echo;

public class EchoRequestValidator : AbstractValidator<EchoRequest>, IRequestValidator<EchoRequest>
{
    private ILogger<EchoRequestValidator> _logger;
    public EchoRequestValidator(ILogger<EchoRequestValidator> logger)
    {
        _logger = logger;
    }

    public Result<EchoRequest, ValidationError> Validate(string request)
    {
        EchoRequest model;
        try
        {
            model = JsonSerializer.Deserialize<EchoRequest>(request);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing request body");
            return new ValidationError.Problem(TypedResults<EchoResponse>.Problem(statusCode: 400, title: "Error deserializing request body", detail: ex.Message));
        }

        var validationResult = Validate(model);
        if (!validationResult.IsValid)
        {
            return new ValidationError.Problem(TypedResults<EchoResponse>.Problem(statusCode: 400, title: "Validation error", detail: JsonSerializer.Serialize(validationResult.ToDictionary())));
        }

        return model;
    }
}
