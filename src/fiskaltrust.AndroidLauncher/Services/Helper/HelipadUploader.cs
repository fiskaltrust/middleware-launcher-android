using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Models;
using fiskaltrust.AndroidLauncher.Services.Helper.Exceptions;
using fiskaltrust.Api.PosSystem.Core;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Timers;

[assembly: InternalsVisibleTo("fiskaltrust.CloudCashbox.UnitTest")]
namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public class HelipadUploader : IHelipadUploader
    {
        private readonly IHelipadService _helipadService;
        private readonly ILogger<HelipadUploader> _logger;
        private System.Timers.Timer _uploadTimer = null;

        private readonly Dictionary<JournalType, string> _readEntities = new Dictionary<JournalType, string> {
            { JournalType.ActionJournal, "actionjournal" },
            { JournalType.ReceiptJournal, "receiptjournal" },
            { JournalType.QueueItem, "queueitem" },
        };

        private readonly CashBoxCredentials _cashBoxCredentials;
        private readonly HelipadHelperConfiguration _helipadHelperConfiguration;
        private readonly PosSystemApiCore _posSystemApiCore;
      
        private AdaptiveRecordCounts RecordCounts;

        public HelipadUploader(IHelipadService helipadService, ILogger<HelipadUploader> logger, CashBoxCredentials cashBoxCredentials, ftCashBoxConfiguration configuration, PosSystemApiCore posSystemApiCore)//IMiddlewareCache middlewareCache, CloudCashboxMetrics metrics
        {
            _helipadService = helipadService;
            _logger = logger;
            _cashBoxCredentials = cashBoxCredentials;
            var helipadhelperConfigurations = configuration?.helpers.FirstOrDefault(x => x.Package == HelperPackages.Helipad)?.Configuration;
            _helipadHelperConfiguration = HelipadHelperConfiguration.FromDictionary(helipadhelperConfigurations);
            _posSystemApiCore = posSystemApiCore;
        }

        public void Start()
        {
            _uploadTimer = new System.Timers.Timer
            {
                AutoReset = false,
                Interval = 30 * 1000
            };
            _uploadTimer.Elapsed += UploadTimer_Elapsed;
            _uploadTimer.Start();
        }

        public void Stop() => _uploadTimer?.Stop();

        private async void UploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await ProcessUpload().ConfigureAwait(false);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Failed to process upload for cashboxid: {cashbox.id}, queueid: {queue.id}", _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
            }
            _uploadTimer.Start();
        }
        public async Task ProcessUpload()
        {

            var recordCounts = GetRecordCounts(_helipadHelperConfiguration);
            await TestwithEchoRequest().ConfigureAwait(false);

            try
            {
                await UploadConfigurationEntities().ConfigureAwait(false);
            }
            catch (Exception x)
            {
                _logger.LogWarning(x, "Error while pushing configuration data for queue {QueueId}", _cashBoxCredentials.QueueId);
                recordCounts.StartRecordCount = recordCounts.StartRecordCount / 2 + 1;
            }

            // A run in which any entity had to shrink (or pin) the shared record count must not grow it again.
            var recordCountAdjusted = false;
            long largestPayload = 0;
            foreach (var entity in _readEntities)
            {
                try
                {
                    var uploadedBytes = await UploadEntityAsync(entity, recordCounts.StartRecordCount, rc => { recordCounts.StartRecordCount = rc; recordCountAdjusted = true; }).ConfigureAwait(false);
                    largestPayload = Math.Max(largestPayload, uploadedBytes);

                }
                catch (Exception x)
                {
                    _logger.LogError(x, "Failed to upload entity. cashboxid: {cashbox.id}, queueid: {queue.id}, entity: {entity}", _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId, entity.Value);
                }
            }

            if (!recordCountAdjusted && largestPayload > 0)
            {
                recordCounts.StartRecordCount = NextRecordCount(recordCounts.StartRecordCount, largestPayload, _helipadHelperConfiguration.MaxUploadBytes, _helipadHelperConfiguration.MaxReadCount);
            }
        }

        private async Task TestwithEchoRequest()
            {
                Dictionary<string, string> _headers = new Dictionary<string, string>
                {
                    { "x-cashbox-id", _cashBoxCredentials.CashBoxId.ToString() },
                    { "x-cashbox-accesstoken", _cashBoxCredentials.AccessToken },
                    { "x-operation-id", Guid.NewGuid().ToString() },
                };
                var request = new PosSystemApiRequest
                {
                    Method = "POST",
                    PathAndQuery = "/v2/echo",
                    Headers = _headers,
                    Body = JsonConvert.SerializeObject(new EchoRequest() { Message = "Hello World!" }),
                };

                if (!(await _posSystemApiCore.HandleAsync(request).ConfigureAwait(false)).IsSuccess)
                {
                    throw new Exception("Sending echo to middleware returned null.");
                }
            }
        

        /// <summary>
        /// Scales the record count so the largest payload of the run lands at half the upload limit. A fixed step
        /// (+50) against the quartering on an oversized payload oscillates for any record size above a few hundred
        /// KB, alternately succeeding and reading a full limit's worth of data just to abort; the proportional step
        /// converges in one run. Nothing is grown after an empty run since there is no payload to scale from.
        /// </summary>
        internal static int NextRecordCount(int recordCount, long largestPayload, long maxUploadBytes, int maxReadCount)
        {
            var target = maxUploadBytes / 2;
            var next = (long)recordCount * target / Math.Max(1, largestPayload);
            return (int)Math.Clamp(next, 1, Math.Max(1, maxReadCount));
        }

        /// <summary>
        /// Returns the adapted record counts for a cashbox. They are seeded from the configuration the first time a
        /// cashbox is seen, kept across configuration reloads as long as the configured start values are unchanged
        /// (so a reload does not restart the adaptation with the large configured batch), re-seeded when an operator
        /// changes the configured values, and clamped to the configured maximum.
        /// </summary>
        private AdaptiveRecordCounts GetRecordCounts(HelipadHelperConfiguration configuration)
        {
            if (RecordCounts == null)
            {
                RecordCounts = AdaptiveRecordCounts.FromConfiguration(configuration);
            }
            else {
                RecordCounts = RecordCounts.IsSeededFrom(configuration) ? RecordCounts.ClampTo(configuration) : AdaptiveRecordCounts.FromConfiguration(configuration);
            }
            return RecordCounts;
        }

        public async Task<long> UploadEntityAsync(KeyValuePair<JournalType, string> entity, int startRecordCount, Action<int> setRecordCount)
        {
            try
            {
                return await UploadEntity(entity, startRecordCount);
            }
            catch (Exception x) when (x is JournalTooLargeException or HelipadException { StatusCode: HttpStatusCode.RequestEntityTooLarge })
            {
                // Either the local limit cut the export short or Helipad answered 413; the message says which.
                _logger.LogWarning(x, "Payload of entity ({EntityKey}, {EntityValue}) with {RecordCount} records is too large (limit {MaxUploadBytes} bytes), reducing the record count. cashboxid: {cashbox.id}, queueid: {queue.id}", entity.Key, entity.Value, startRecordCount, _helipadHelperConfiguration.MaxUploadBytes, _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
                HandleOversizedPayload(entity, startRecordCount, setRecordCount);
            }
            catch (HelipadException x)
            {
                _logger.LogError(x, "Error while pushing entity data ({EntityKey}, {EntityValue}) - HttpStatusCode: {StatusCode}", entity.Key, entity.Value, x.StatusCode);
                setRecordCount(startRecordCount / 2 + 1);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Error while pushing entity data ({EntityKey}, {EntityValue})", entity.Key, entity.Value);
                setRecordCount(startRecordCount / 2 + 1);
            }
            return 0;
        }

        private void HandleOversizedPayload(KeyValuePair<JournalType, string> entity, int startRecordCount, Action<int> setRecordCount)
        {
            if (startRecordCount <= 1)
            {
                // A single record was refused (by Helipad, or by the runaway ceiling). Retrying cannot make it
                // smaller and the pointer stays in front of it, so the entity is blocked until the record or the
                // limit changes. Pin the count at 1 so the other entities' successes do not grow it and trigger a
                // shrink cycle every run, count it, and do not requeue: the next sign request re-queues the cashbox
                // anyway.
                _logger.LogError("A single record of entity ({EntityKey}, {EntityValue}) is too large to upload and blocks the entity. cashboxid: {cashbox.id}, queueid: {queue.id}", entity.Key, entity.Value, _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
                setRecordCount(1);
                return;
            }
            setRecordCount(ShrinkRecordCountForOversizedPayload(startRecordCount));

            // Nothing was uploaded, so the pointer did not move and nobody else will schedule this cashbox again.
            // Requeue it explicitly so the next run retries with the smaller batch.
        }

        internal static int ShrinkRecordCountForOversizedPayload(int recordCount) => Math.Max(1, recordCount / 4);

        private async Task<long> UploadEntity(KeyValuePair<JournalType, string> entity, int startRecordCount)
        {
            var pointer = await _helipadService.GetPointerAsync(_cashBoxCredentials, _cashBoxCredentials.QueueId, entity.Value).ConfigureAwait(false);

            var request = new JournalRequest()
            {
                ftJournalType = entity.Key,
                From = pointer + 1,
                To = -startRecordCount
            };

            // A single record cannot be split, so the configured limit (a guess at Helipad's) is not applied to it and
            // Helipad decides; the hard ceiling only guards against a runaway export, e.g. a middleware that ignores
            // the requested record count.
            using var stream = await ReadJournalAsync(request, startRecordCount <= 1 ? HelipadHelperConfiguration.MaxUploadBytesCeiling : _helipadHelperConfiguration.MaxUploadBytes).ConfigureAwait(false);
            // Read before the upload: the service hands the stream to an HttpRequestMessage that disposes it.
            var payloadBytes = stream.Length;
            
            if (payloadBytes == 0)
            {
                return 0;
            }

            await _helipadService.UploadDataAsync(stream, _cashBoxCredentials, _cashBoxCredentials.QueueId, entity.Value).ConfigureAwait(false);
            return payloadBytes;
        }

        /// <summary>
        /// Reads the journal chunk by chunk and stops as soon as the payload would exceed <paramref name="maxBytes"/>,
        /// so a cashbox with a large backlog can never pull an unbounded amount of data into memory. The caller is
        /// expected to retry with a smaller record count. The chunks are joined into one exactly sized buffer at the
        /// end; writing them into a growing <see cref="MemoryStream"/> would allocate about twice the payload again
        /// in doubling steps on the large object heap.
        /// </summary>
        internal async Task<MemoryStream> ReadJournalAsync(ifPOS.v2.JournalRequest journalRequest, long maxBytes)
        {
            var chunks = new List<byte[]>();
            long length = 0;
            ResponseBody.FileStream fileStream = await SendJournalRequest(journalRequest);

            await foreach (var chunk in fileStream.Chunks)
            {
                if (chunk is null || chunk.Length == 0)
                {
                    continue;
                }

                if (length + chunk.Length > maxBytes)
                {
                    throw new JournalTooLargeException(journalRequest.ftJournalType, length + chunk.Length, maxBytes);
                }

                chunks.Add(chunk);
                length += chunk.Length;
            }


            var buffer = new byte[length];
            var offset = 0;
            foreach (var chunk in chunks)
            {
                chunk.CopyTo(buffer, offset);
                offset += chunk.Length;
            }
            return new MemoryStream(buffer, writable: false);
        }
            private async Task<ResponseBody.FileStream> SendJournalRequest(JournalRequest journalRequest)
            {
                Dictionary<string, string> _headers = new Dictionary<string, string>
                {
                    { "x-cashbox-id", _cashBoxCredentials.CashBoxId.ToString() },
                    { "x-cashbox-accesstoken", _cashBoxCredentials.AccessToken },
                };
                var request = new PosSystemApiRequest
                {
                    Method = "POST",
                    PathAndQuery = "/v2/journal",
                    Headers = _headers,
                    Body = JsonConvert.SerializeObject(journalRequest),
                };
                var response = await _posSystemApiCore.HandleAsync(request);
                if (response.Content is not ResponseBody.FileStream fileStream)
                {
                    throw new Exception($"Unexpected journal response body type: {response.Content?.GetType().Name ?? "null"}");
                }

                return fileStream;
            }

        public async Task UploadConfigurationEntities()
        {
            var serializer = new JsonSerializer()
            {
                FloatParseHandling = FloatParseHandling.Decimal
            };

            var request = new JournalRequest()
            {
                ftJournalType = JournalType.Configuration,
                From = 0,
                To = 0,
            };

            // The configuration journal is always exported as a whole (From = 0, To = 0), so there is no record count
            // that could be reduced on retry. It is not subject to the upload limit.
            using (var stream = await ReadJournalAsync(request, long.MaxValue))
            {
                using (var sr = new StreamReader(stream))
                using (var jr = new JsonTextReader(sr))
                {
                    var data = new JsonSerializer() { FloatParseHandling = FloatParseHandling.Decimal }.Deserialize<WorkerConfiguration>(jr);
                    if (data != null)
                    {
                        await _helipadService.UploadQueueListAsync(_cashBoxCredentials, data.QueueList);
                        await _helipadService.UploadeQueuePLListAsync(_cashBoxCredentials, data.QueuePLList);
                        await _helipadService.UploadSignatureCreationUnitPLListAsync(_cashBoxCredentials, data.SignaturCreationUnitPLList);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Per-cashbox record counts the uploader adapts at runtime, together with the configured values they were
    /// seeded from so a changed configuration can be told apart from a mere reload.
    /// </summary>
    internal sealed class AdaptiveRecordCounts
    {
        public int ConfiguredStartRecordCount { get; private init; }
        public int StartRecordCount { get; set; }

        public static AdaptiveRecordCounts FromConfiguration(HelipadHelperConfiguration configuration) => new AdaptiveRecordCounts
        {
            ConfiguredStartRecordCount = configuration.StartRecordCount,
            StartRecordCount = configuration.StartRecordCount,
        };

        public bool IsSeededFrom(HelipadHelperConfiguration configuration)
            => ConfiguredStartRecordCount == configuration.StartRecordCount;

        public AdaptiveRecordCounts ClampTo(HelipadHelperConfiguration configuration)
        {
            StartRecordCount = Math.Clamp(StartRecordCount, 1, Math.Max(1, configuration.MaxReadCount));
            return this;
        }
    }
}
