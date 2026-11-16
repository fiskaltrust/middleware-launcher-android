using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Runtime.InteropServices;
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

        public Task<(Stream? data, string? contentType, string? error)> JournalAsync(MiddlewareRequestOptions requestOptions, ifPOS.v1.JournalRequest request)
        {
            // Returns right away: a background task pumps the middleware's chunks into the pipe while the caller reads
            // the other end. The pipe pauses the writer once ~64 KB are unread, so only a few chunks are ever in memory.
            var pipe = new Pipe();
            _ = Task.Run(() => PumpJournalAsync(request, pipe.Writer));
            return Task.FromResult<(Stream?, string?, string?)>((pipe.Reader.AsStream(), "application/octet-stream", null));
        }

        private async Task PumpJournalAsync(ifPOS.v1.JournalRequest request, PipeWriter writer)
        {
            try
            {
                await foreach (var response in _pos.JournalAsync(request).ConfigureAwait(false))
                {
                    if (response?.Chunk is not { Count: > 0 } chunk)
                    {
                        continue;
                    }

                    // Copies straight from the list's backing array into the pipe's buffer, no ToArray() in between.
                    writer.Write(CollectionsMarshal.AsSpan(chunk));

                    // Waits here while the reader is behind (back-pressure). IsCompleted means the reader disposed the
                    // stream, so stop pulling from the middleware.
                    var flush = await writer.FlushAsync().ConfigureAwait(false);
                    if (flush.IsCompleted)
                    {
                        break;
                    }
                }
                await writer.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The reader gets this exception from its next Read on the stream.
                await writer.CompleteAsync(ex).ConfigureAwait(false);
            }
        }

        public async Task<(Stream? data, string? contentType, string? error)> JournalV2Async(MiddlewareRequestOptions requestOptions, ifPOS.v2.JournalRequest request)
        {
            try
            {
                var (contentType, pipeReader) = await _posV2.Journal(JsonSerializer.Serialize(request)).ConfigureAwait(false);
                // AsStream() advances the reader past every byte it returns and completes the reader when the stream
                // is disposed, so no manual AdvanceTo/CompleteAsync is needed.
                return (pipeReader.AsStream(), contentType.MediaType, null);
            }
            catch (Exception ex)
            {
                return (null, null, ex.Message);
            }
        }
    }
}
