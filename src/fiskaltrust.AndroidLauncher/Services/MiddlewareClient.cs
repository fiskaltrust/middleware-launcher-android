using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareClient : IMiddlewareClient
    {
        private readonly IPOS _pos;

        public MiddlewareClient(IPOS pos)
        {
            _pos = pos;
        }

        public async Task<(ifPOS.v2.EchoResponse?, string? error)> EchoV2Async(ifPOS.v2.EchoRequest request)
        {
            try
            {
                var response = await _pos.EchoAsync(new EchoRequest
                {
                    Message = request.Message
                });
                return (new ifPOS.v2.EchoResponse
                {
                    Message = response.Message
                }, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(ifPOS.v2.ReceiptResponse?, string? error)> SignV2Async(ifPOS.v2.ReceiptRequest request)
        {
            try
            {
                var response = await _pos.SignAsync(fiskaltrust.Api.POS.Signing.ReceiptRequestHelper.ConvertToV1(request));
                return (fiskaltrust.Api.POS.Signing.ReceiptRequestHelper.ConvertToV2(response), null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }
    }
}