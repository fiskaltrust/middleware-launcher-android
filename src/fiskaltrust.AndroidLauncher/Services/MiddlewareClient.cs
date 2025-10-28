using fiskaltrust.Api.POS.Signing;
using fiskaltrust.Api.PosSystemLocal.v2;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareClient : IMiddlewareClient
    {
        private readonly IPOS _pos;
        private readonly string _market;

        public MiddlewareClient(IPOS pos, string market)
        {
            _pos = pos;
            _market = market;
        }

        public string CountryCode => _market;

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

        public async Task<(byte[]?, string? error)> JournalV2Async(ifPOS.v2.JournalRequest request)
        {
            try
            {
                var response = await _pos.JournalAsync(new JournalRequest
                {
                    From = request.From,
                    To = request.To,
                    ftJournalType = (long)request.ftJournalType
                }).ToListAsync();
                return (response.SelectMany(x => x.Chunk).ToArray(), null);
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
                if (_market == "DE")
                {
                    var receiptRequestV1 = MappingFactory.ConvertV2ToV1Request(request, _market?.ToUpper());
                    var response = await _pos.SignAsync(receiptRequestV1);
                    return (MappingFactory.ConvertV1ToV2Response(response, _market), null);
                }
                else 
                {
                    var response = await _pos.SignAsync(ReceiptRequestHelper.ConvertToV1(request));
                    return (fiskaltrust.Api.POS.Signing.ReceiptRequestHelper.ConvertToV2(response), null);
                }
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }
    }
}