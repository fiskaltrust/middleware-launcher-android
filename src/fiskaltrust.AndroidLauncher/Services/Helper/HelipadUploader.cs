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
        private System.Timers.Timer? _uploadTimer;
        private TaskCompletionSource? _currentRun = null;

        private readonly Dictionary<JournalType, string> _readEntities = new Dictionary<JournalType, string> {
            { JournalType.ActionJournal, "actionjournal" },
            { JournalType.ReceiptJournal, "receiptjournal" },
            { JournalType.QueueItem, "queueitem" },
        };

        private readonly CashBoxCredentials _cashBoxCredentials;
        private readonly HelipadHelperConfiguration _helipadHelperConfiguration;
        private readonly PosSystemApiCore _posSystemApiCore;

        private AdaptiveRecordCounts? _recordCounts;

        public HelipadUploader(IHelipadService helipadService, ILogger<HelipadUploader> logger, CashBoxCredentials cashBoxCredentials, ftCashBoxConfiguration configuration, PosSystemApiCore posSystemApiCore)
        {
            _helipadService = helipadService;
            _logger = logger;
            _cashBoxCredentials = cashBoxCredentials;
            var helipadhelperConfigurations = configuration?.helpers?.FirstOrDefault(x => x.Package == HelperPackages.Helipad)?.Configuration;
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

        /// <summary>
        /// Stops scheduling new runs and, if a run is still in progress, waits until it has finished, so the caller
        /// does not tear down the middleware the run is reading from. <paramref name="cancellationToken"/> bounds the
        /// wait (e.g. the host's shutdown timeout); when it fires an <see cref="OperationCanceledException"/> is thrown.
        /// </summary>
        public async Task Stop(CancellationToken cancellationToken = default)
        {
            _uploadTimer?.Stop();
            _uploadTimer = null;
            _logger.LogInformation("Waiting for the Helipad upload to finish. cashboxid: {cashbox.id}, queueid: {queue.id}", _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
            if (_currentRun is not null && !_currentRun.Task.IsCompleted)
            {
                await _currentRun.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            else 
            {
                try
                {
                    await ProcessUpload().ConfigureAwait(false);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "Failed to process upload for cashboxid: {cashbox.id}, queueid: {queue.id}", _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
                }
            }

        }

        private async void UploadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Stop() has already run: Timer.Stop() does not cancel a tick that was already queued on the thread pool, so
            // such a late tick must not start a new run that Stop() no longer waits for.
            if (_uploadTimer == null) return;

            _currentRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                await ProcessUpload().ConfigureAwait(false);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Failed to process upload for cashboxid: {cashbox.id}, queueid: {queue.id}", _cashBoxCredentials.CashBoxId, _cashBoxCredentials.QueueId);
            }
            finally
            {
                _currentRun.TrySetResult();
            }
            _uploadTimer?.Start();
        }
        public async Task ProcessUpload()
        {

            var recordCounts = GetRecordCounts(_helipadHelperConfiguration);
            await SendEchoRequestAsync().ConfigureAwait(false);

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

        private async Task SendEchoRequestAsync()
        {
            var headers = new Dictionary<string, string>
            {
                { "x-cashbox-id", _cashBoxCredentials.CashBoxId.ToString() },
                { "x-cashbox-accesstoken", _cashBoxCredentials.AccessToken },
                { "x-operation-id", Guid.NewGuid().ToString() },
            };
            var request = new PosSystemApiRequest
            {
                Method = "POST",
                PathAndQuery = "/v2/echo",
                Headers = headers,
                Body = JsonConvert.SerializeObject(new EchoRequest { Message = "Hello World!" }),
            };

            if (!(await _posSystemApiCore.HandleAsync(request).ConfigureAwait(false)).IsSuccess)
            {
                throw new Exception("Sending echo to PosSystem api returned null.");
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
            if (_recordCounts == null)
            {
                _recordCounts = AdaptiveRecordCounts.FromConfiguration(configuration);
            }
            else
            {
                _recordCounts = _recordCounts.IsSeededFrom(configuration) ? _recordCounts.ClampTo(configuration) : AdaptiveRecordCounts.FromConfiguration(configuration);
            }
            return _recordCounts;
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
        internal async Task<MemoryStream> ReadJournalAsync(ifPOS.v2.JournalRequest journalRequest, long maxBytes)
        {
            var byteStream = await SendJournalRequest(journalRequest);

            using var stream = new MemoryStream();
            using (var source = byteStream.Content)
            {
                var chunk = new byte[81920];
                long length = 0;
                int read;
                while ((read = await source.ReadAsync(chunk).ConfigureAwait(false)) > 0)
                {
                    if (length + read > maxBytes)
                    {
                        throw new JournalTooLargeException(journalRequest.ftJournalType, length + read, maxBytes);
                    }
                    stream.Write(chunk, 0, read);
                    length += read;
                }
            }
            stream.Position = 0;
            return stream;
        }



        private async Task<ResponseBody.ByteStream> SendJournalRequest(JournalRequest journalRequest)
        {
            var headers = new Dictionary<string, string>
            {
                { "x-cashbox-id", _cashBoxCredentials.CashBoxId.ToString() },
                { "x-cashbox-accesstoken", _cashBoxCredentials.AccessToken },
            };
            var request = new PosSystemApiRequest
            {
                Method = "POST",
                PathAndQuery = "/v2/journal",
                Headers = headers,
                Body = JsonConvert.SerializeObject(journalRequest),
            };
            var response = await _posSystemApiCore.HandleAsync(request);
            if (response.Content is not ResponseBody.ByteStream fileStream)
            {
                throw new Exception($"Unexpected journal response body type: {response.Content?.GetType().Name ?? "null"}");
            }

            return fileStream;
        }

        public async Task UploadConfigurationEntities()
        {
            var journalRequest = new ifPOS.v2.JournalRequest()
            {
                ftJournalType = JournalType.Configuration,
                From = 0,
                To = 0,
            };

            // The configuration journal is always exported as a whole (From = 0, To = 0), so there is no record count
            // that could be reduced on retry. It is not subject to the upload limit.
            using (var stream = await ReadJournalAsync(journalRequest, long.MaxValue).ConfigureAwait(false))
            {
                using (var sr = new StreamReader(stream))
                using (var jr = new JsonTextReader(sr))
                {
                    var data = new JsonSerializer() { FloatParseHandling = FloatParseHandling.Decimal }.Deserialize<WorkerConfiguration>(jr);
                    if (data != null)
                    {
                        await _helipadService.UploadQueueListAsync(_cashBoxCredentials, data.QueueList).ConfigureAwait(false);
                        await _helipadService.UploadQueuePLListAsync(_cashBoxCredentials, data.QueuePLList).ConfigureAwait(false);
                        await _helipadService.UploadSignatureCreationUnitPLListAsync(_cashBoxCredentials, data.SignaturCreationUnitPLList).ConfigureAwait(false);
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
