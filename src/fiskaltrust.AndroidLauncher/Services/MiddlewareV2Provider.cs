using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Services
{
    public class MiddlewareV2Provider : IMiddlewareProvider
    {
        private readonly ftCashBoxConfiguration _configuration;

        private readonly Guid _cashboxId;
        public Guid CashboxId => _cashboxId;
        private readonly string _accessToken;
        public string AccessToken => _accessToken;
        private readonly bool _isSandbox;
        private readonly string _countryCode;
        private readonly ILoggerFactory _loggerFactory;
        private readonly LogLevel _logLevel;
        private readonly List<IHelper> _helpers = new();

        private POSV2? _pos;

        public POSV2? POSV2 => _pos;

        public Api.PosSystem.Core.Interfaces.IMiddlewareClient MiddlewareClient => MiddlewareClientAndroid.FromV2(_pos!);

        public PackageConfiguration? QueueConfiguration { get; private set; }

        public MiddlewareV2Provider(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, string countryCode, bool isSandbox, ILoggerFactory loggerFactory, LogLevel logLevel)
        {
            _configuration = configuration;
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _countryCode = countryCode;
            _isSandbox = isSandbox;
            _loggerFactory = loggerFactory;
            _logLevel = logLevel;
        }

        public async Task StopAsync()
        {
            foreach (var helper in _helpers)
            {
                try
                {
                    helper.StopBegin();
                    helper.StopEnd();
                }
                catch (Exception x)
                {
                    Log.Logger.Warning(x, "Failed to stop helper. cashboxid: {cashbox.id}", _cashboxId);
                }
            }
            _helpers.Clear();
        }
        public async Task StartAsync()
        {
            if (_configuration.ftQueues.Length != 1)
            {
                throw new ArgumentException("Only one queue is supported in V2.");
            }
            if (_configuration.ftSignaturCreationDevices.Length > 1)
            {
                throw new ArgumentException("Only one signatur creation device is supported in V2.");
            }

            try
            {
                var queueConfiguration = _configuration.ftQueues[0];
                if (queueConfiguration.Package != SupportedQueues.Package_SQLite_Queue)
                {
                    throw new ArgumentException($"The Android launcher currently only supports the SQLite queue");
                }
                var queueProvider = new SQLiteQueueProvider();
                _pos = queueProvider.CreatePOSV2(CashboxId, AccessToken, queueConfiguration, _configuration.ftSignaturCreationDevices.FirstOrDefault(), _countryCode, _loggerFactory);
                Log.Logger.Information("Queue created. cashboxid: {cashbox.id}, queueId: {queue.id}", _cashboxId, queueConfiguration.Id);
            }
            catch (Exception x)
            {
                Log.Logger.Error(x, "Failed to create worker. cashboxid: {cashbox.id}", _cashboxId);
                throw;
            }

            await InitializeHelipadHelperAsync();
        }

        private async Task InitializeHelipadHelperAsync()
        {
            // The upload to Helipad is not needed to sign receipts, so a failing helper must not stop the queue from starting.
            try
            {
                var helipadHelperProvider = new HelipadHelperProvider();
                var helper = await Task.Run(() => helipadHelperProvider.CreateHelper(_configuration, _accessToken, _isSandbox, _logLevel, new List<IPOS> { new POSV2JournalAdapter(_pos!) }));
                helper.StartBegin();
                helper.StartEnd();
                _helpers.Add(helper);
            }
            catch (Exception x)
            {
                Log.Logger.Error(x, "Failed to start the Helipad helper; receipts will not be uploaded. cashboxid: {cashbox.id}", _cashboxId);
            }
        }
    }
}
