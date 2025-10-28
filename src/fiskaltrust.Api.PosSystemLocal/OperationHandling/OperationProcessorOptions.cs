using fiskaltrust.Api.PosSystemLocal.Models;


namespace fiskaltrust.Api.PosSystemLocal.OperationHandling;

public record OperationProcessorOptions
{
    public Guid CashBoxID { get; set; }
    public string CashBoxAccessToken { get; set; }
    public Guid OperationID { get; set; }
    public Guid? PosSystemID { get; set; }
    public string? TerminalID { get; set; }

    public static OperationProcessorOptions FromContext(PosSystemApiRequest request) => new OperationProcessorOptions
    {
        CashBoxID = Guid.Parse(request.Headers["x-cashbox-id"]),
        CashBoxAccessToken = request.Headers["x-cashbox-accesstoken"],
        PosSystemID = request.Headers.ContainsKey("x-possystem-id") && Guid.TryParse(request.Headers["x-possystem-id"], out var posSystemId) ? posSystemId : null,
        TerminalID = request.Headers.ContainsKey("x-terminal-id") ? request.Headers["x-terminal-id"] : null,
        OperationID = Guid.Parse(request.Headers["x-operation-id"])
    };
}
