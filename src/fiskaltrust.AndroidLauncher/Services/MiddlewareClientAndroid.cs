using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2;
using System.Buffers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareClientAndroid : IMiddlewareClient
    {
        private readonly IPOS _pos;
        private readonly POSV2 _posV2;

        public MiddlewareClientAndroid(IPOS? pos, POSV2? posV2)
        {
            _pos = pos!;
            _posV2 = posV2!;
        }

        public static MiddlewareClientAndroid FromV1(IPOS pos) => new(pos, null);
        public static MiddlewareClientAndroid FromV2(POSV2 posV2) => new(null, posV2);

        public async Task<(ifPOS.v1.EchoResponse?, string? error)> EchoAsync(MiddlewareRequestOptions requestOptions, ifPOS.v1.EchoRequest request)
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

        public async Task<(IAsyncEnumerable<ifPOS.v1.JournalResponse>? data, string? contentType, string? error)> JournalAsync(MiddlewareRequestOptions requestOptions, ifPOS.v1.JournalRequest request)
        {
            try
            {
                return (_pos.JournalAsync(request), "application/octet-stream", null);
            }
            catch (Exception ex)
            {
                return (null, null, ex.Message);
            }
        }

        private async Task<(ContentType contentType, IAsyncEnumerable<ifPOS.v1.JournalResponse> response)> JournalResponsesAsync(ifPOS.v2.JournalRequest request)
        {
            var (contentType, pipeReader) = await _posV2.Journal(JsonSerializer.Serialize(request)).ConfigureAwait(false);
            return (contentType, ReadJournalResponsesAsync(pipeReader));
        }

        private static async IAsyncEnumerable<ifPOS.v1.JournalResponse> ReadJournalResponsesAsync(System.IO.Pipelines.PipeReader pipeReader)
        {
            try
            {
                while (true)
                {
                    var result = await pipeReader.ReadAsync();
                    var buffer = result.Buffer;

                    if (!buffer.IsEmpty)
                    {
                        yield return new ifPOS.v1.JournalResponse
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

        public async Task<(IAsyncEnumerable<ifPOS.v1.JournalResponse>? data, string? contentType, string? error)> JournalV2Async(MiddlewareRequestOptions requestOptions, ifPOS.v2.JournalRequest request)
        {
            try
            {
                var (contentType, responses) = await JournalResponsesAsync(request);
                return (responses, contentType.MediaType, null);
            }
            catch (Exception ex)
            {
                return (null, null, ex.Message);
            }
        }
    }
}
