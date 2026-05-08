using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Signing;
using fiskaltrust.ifPOS.v1;
using System.Text;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareClientAndroid : IMiddlewareClient
    {
        private readonly IPOS _pos;
        private readonly string _market;

        public MiddlewareClientAndroid(IPOS pos, string market)
        {
            _pos = pos;
            _market = market;
        }

        public string CountryCode => _market;

        public async Task<(EchoResponse?, string? error)> EchoAsync(MiddlewareRequestOptions requestOptions, EchoRequest request)
        {
            try
            {
                var response = await _pos.EchoAsync(request);
                return (response, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }
        public async Task<(ifPOS.v2.EchoResponse?, string? error)> EchoV2Async(MiddlewareRequestOptions requestOptions, ifPOS.v2.EchoRequest request)
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

        public async Task<(byte[]?, string? contentType, string? error)> JournalAsync(MiddlewareRequestOptions requestOptions, JournalRequest request)
        {
            List<JournalResponse> response;
            try
            {
                response = await _pos.JournalAsync(request).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            var Bytes = response.SelectMany(x => x.Chunk).ToArray();
            var content = Encoding.UTF8.GetString(Bytes);
            if (content.StartsWith("<?xml"))
                return (Bytes, contentType: "application/xml", null);
            return (Bytes, contentType: "application/json", null);
        }
        public async Task<(byte[]?, string? contentType, string? error)> JournalV2Async(MiddlewareRequestOptions requestOptions, ifPOS.v2.JournalRequest request)
        {
            List<JournalResponse> response;
            try
            {
                response = await _pos.JournalAsync(new JournalRequest
                {
                    From = request.From,
                    To = request.To,
                    ftJournalType = (long)request.ftJournalType
                }).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            var Bytes = response.SelectMany(x => x.Chunk).ToArray();
            var content = Encoding.UTF8.GetString(Bytes);
            if (content.StartsWith("<?xml"))
                return (Bytes, contentType: "application/xml", null);
            return (Bytes, contentType: "application/json", null);
        }
        
        public async Task<(ifPOS.v1.ReceiptResponse?, string? error)> SignAsync(MiddlewareRequestOptions requestOptions, ifPOS.v1.ReceiptRequest request)
        {
            try
            {
               var response = await _pos.SignAsync(request);
               return (response, null);
               
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(ifPOS.v2.ReceiptResponse?, string? error)> SignV2Async(MiddlewareRequestOptions requestOptions, ifPOS.v2.ReceiptRequest request)
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
                    return (ReceiptRequestHelper.ConvertToV2(response), null);
                }
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }
    }
}