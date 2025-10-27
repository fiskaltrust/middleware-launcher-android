using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.ifPOS.v2;
using FluentValidation;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;

namespace fiskaltrust.Api.POS.v2.Sign;

public class ReceiptRequestV2Validator : AbstractValidator<ReceiptRequest>, IRequestValidator<ReceiptRequest>
{
    private readonly ILogger<ReceiptRequestV2Validator> _logger;

    public ReceiptRequestV2Validator(ILogger<ReceiptRequestV2Validator> logger)
    {
        _logger = logger;
        RuleFor(x => x.cbReceiptReference).NotNull();
        RuleFor(x => x.cbReceiptMoment).NotNull();
        RuleFor(x => x.cbChargeItems).NotNull();
        RuleFor(x => x.cbPayItems).NotNull();

        RuleForEach(x => x.cbChargeItems).SetValidator(new ChargeItemValidator());
        RuleForEach(x => x.cbPayItems).SetValidator(new PayItemValidator());
    }

    public Result<ReceiptRequest, ValidationError> Validate(string request)
    {
        ReceiptRequest model = null;
        try
        {
            model = JsonSerializer.Deserialize<ReceiptRequest>(request, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing request body");
            return new ValidationError.Problem(TypedResults<ReceiptResponse>.Problem(statusCode: 400, title: "Error deserializing request body", detail: ex.Message));
        }

        var validationResult = Validate(model);
        if (!validationResult.IsValid)
        {
            return new ValidationError.Problem(TypedResults<ReceiptResponse>.Problem(statusCode: 400, title: "Validation error", detail: JsonSerializer.Serialize(validationResult.ToDictionary())));
        }
        return model;
    }
}

public class PayItemValidator : AbstractValidator<PayItem>
{
    public PayItemValidator()
    {
        RuleFor(x => x.Description).NotNull();
        RuleFor(x => x.Amount).NotNull();
        RuleFor(x => x.ftPayItemCase).NotNull();
    }
}

public class ChargeItemValidator : AbstractValidator<ChargeItem>
{
    public ChargeItemValidator()
    {
        RuleFor(x => x.Quantity).NotNull();
        RuleFor(x => x.Description).NotNull();
        RuleFor(x => x.Amount).NotNull();
        RuleFor(x => x.VATRate).NotNull();
        RuleFor(x => x.ftChargeItemCase).NotNull();
    }
}
