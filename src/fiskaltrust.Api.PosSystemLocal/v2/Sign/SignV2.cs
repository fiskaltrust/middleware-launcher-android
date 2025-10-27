using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.Api.PosSystemLocal.Models;
using fiskaltrust.Api.PosSystemLocal.OperationHandling;
using fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;
using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.ifPOS.v2;
using Microsoft.Extensions.Logging;
using ReceiptRequest = fiskaltrust.ifPOS.v2.ReceiptRequest;

namespace fiskaltrust.Api.POS.v2.Sign;


public class SignRequestState
{
    public Guid OperationID { get; set; }
    public Guid QueueID { get; set; }
    public string State { get; set; }
    public Guid QueueItemID { get; set; }
}

public class SignProcessor(
    IMiddlewareClient middlewareClient,
    ILoggerFactory loggerFactory
) : IOperationProcessor<ReceiptRequest, ReceiptResponse>
{

    public async Task<Result<ReceiptResponse, ProblemDetails>> ProcessAsync(ReceiptRequest request, OperationProcessorOptions options)
    {
        if (!request.ftCashBoxID.HasValue && options.CashBoxID != Guid.Empty)
        {
            request.ftCashBoxID = options.CashBoxID;
        }
        else if (request.ftCashBoxID != options.CashBoxID)
        {
            return TypedResults<ReceiptResponse>.Problem(statusCode: 400, detail: "CashBoxID in request does not match the CashBoxID in the request header.");
        }

        if (!request.ftPosSystemId.HasValue && options.PosSystemID.HasValue)
        {
            request.ftPosSystemId = options.PosSystemID;
        }
        else if (options.PosSystemID.HasValue && request.ftPosSystemId != options.PosSystemID)
        {
            return TypedResults<ReceiptResponse>.Problem(statusCode: 400, detail: "PosSystemID in request does not match the PosSystemID in the request header.");
        }

        if (string.IsNullOrEmpty(request.cbTerminalID) && !string.IsNullOrEmpty(options.TerminalID))
        {
            request.cbTerminalID = options.TerminalID;
        }
        else if (!string.IsNullOrEmpty(options.TerminalID) && request.cbTerminalID != options.TerminalID)
        {
            return TypedResults<ReceiptResponse>.Problem(statusCode: 400, detail: "TerminalID in request does not match the TerminalID in the request header.");
        }

        var result = await middlewareClient.SignV2Async(request);
        if (result.error != null)
        {
            return TypedResults<ReceiptResponse>.Problem(statusCode: 400, detail: result.error);
        }
        return result.Item1;
    }
}

