using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.Cases;

#pragma warning disable CS0618
namespace fiskaltrust.AndroidLauncher.Services.middleware
{
    /// <summary>
    /// Exposes the journal endpoint of a v2 queue (<see cref="POSV2"/>) through the ifPOS v1 <see cref="IPOS"/> interface,
    /// so that v1 consumers like the Helipad helper can read journals from it. Only the journal methods are supported.
    /// </summary>
    public sealed class POSV2JournalAdapter : IPOS
    {
        private const int DefaultChunkSize = 4096;

        private readonly POSV2 _posV2;

        public POSV2JournalAdapter(POSV2 posV2)
        {
            _posV2 = posV2;
        }

        public async IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request)
        {
            var reader = await OpenJournalAsync(request.ftJournalType, request.From, request.To);
            var maxChunkSize = request.MaxChunkSize > 0 ? request.MaxChunkSize : DefaultChunkSize;

            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync();
                    var buffer = result.Buffer;

                    while (buffer.Length > 0)
                    {
                        var chunk = buffer.Slice(0, Math.Min(buffer.Length, maxChunkSize));
                        yield return new JournalResponse { Chunk = chunk.ToArray().ToList() };
                        buffer = buffer.Slice(chunk.End);
                    }

                    reader.AdvanceTo(result.Buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        public Stream Journal(long ftJournalType, long from, long to)
            => OpenJournalAsync(ftJournalType, from, to).GetAwaiter().GetResult().AsStream();

        private async Task<PipeReader> OpenJournalAsync(long ftJournalType, long from, long to)
        {
            var request = new ifPOS.v2.JournalRequest
            {
                ftJournalType = (JournalType)(ulong)ftJournalType,
                From = from,
                To = to
            };
            var (_, reader) = await _posV2.Journal(JsonSerializer.Serialize(request));
            return reader;
        }

        public IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state) => throw new NotSupportedException();
        public Stream EndJournal(IAsyncResult result) => throw new NotSupportedException();

        public Task<ifPOS.v1.ReceiptResponse> SignAsync(ifPOS.v1.ReceiptRequest request) => throw new NotSupportedException("Use the v2 sign endpoint of the queue.");
        public ifPOS.v0.ReceiptResponse Sign(ifPOS.v0.ReceiptRequest data) => throw new NotSupportedException("Use the v2 sign endpoint of the queue.");
        public IAsyncResult BeginSign(ifPOS.v0.ReceiptRequest data, AsyncCallback callback, object state) => throw new NotSupportedException();
        public ifPOS.v0.ReceiptResponse EndSign(IAsyncResult result) => throw new NotSupportedException();

        public Task<EchoResponse> EchoAsync(EchoRequest message) => throw new NotSupportedException("Use the v2 echo endpoint of the queue.");
        public string Echo(string message) => throw new NotSupportedException("Use the v2 echo endpoint of the queue.");
        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state) => throw new NotSupportedException();
        public string EndEcho(IAsyncResult result) => throw new NotSupportedException();
    }
}
