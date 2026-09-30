using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Signing;
using fiskaltrust.ifPOS.v1;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareClientAndroid : IMiddlewareClient
    {
        private readonly IPOS _pos;
        private readonly POSV2 _posV2;
        private readonly string _market;

        public MiddlewareClientAndroid(IPOS? pos, POSV2? posV2, string market)
        {
            _pos = pos!;
            _posV2 = posV2!;
            _market = market;
        }

        public static MiddlewareClientAndroid FromV1(IPOS pos, string market) => new(pos, null, market);
        public static MiddlewareClientAndroid FromV2(POSV2 posV2, string market) => new(null, posV2, market);

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
                var response = JsonSerializer.Deserialize<ifPOS.v2.EchoResponse>(await _posV2.Echo(JsonSerializer.Serialize(request)))!;
                return (response, null);
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
            var response = await JournalResponsesAsync(request).ToListAsync();

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
                var response = JsonSerializer.Deserialize<ifPOS.v2.ReceiptResponse>(await _posV2.Sign(JsonSerializer.Serialize(request)))!;
                return (response, null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        private async IAsyncEnumerable<JournalResponse> JournalResponsesAsync(ifPOS.v2.JournalRequest request)
        {
            var (_, pipeReader) = await _posV2.Journal(JsonSerializer.Serialize(request));

            try
            {
                while (true)
                {
                    var result = await pipeReader.ReadAsync();
                    var buffer = result.Buffer;

                    if (!buffer.IsEmpty)
                    {
                        yield return new JournalResponse
                        {
                            Chunk = buffer.ToArray().ToList()
                        };
                    }

                    // Mark the whole buffer as consumed. Advancing only the "examined" position keeps the bytes in the
                    // pipe, so every following read would hand out the same data again and the journal would be
                    // uploaded as a sequence of ever-growing duplicates.
                    pipeReader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await pipeReader.CompleteAsync();
            }
        }
    }
}