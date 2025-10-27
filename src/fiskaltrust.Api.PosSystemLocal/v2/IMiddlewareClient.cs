using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Api.PosSystemLocal.v2;

public interface IMiddlewareClient
{
    Task<(EchoResponse?, string? error)> EchoV2Async(EchoRequest request);
    Task<(ReceiptResponse?, string? error)> SignV2Async(ReceiptRequest request);
}


