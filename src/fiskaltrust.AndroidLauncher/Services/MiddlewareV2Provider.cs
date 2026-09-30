using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.AndroidLauncher.Services.Queue;
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
        private readonly ILoggerFactory _loggerFactory;

        private POSV2? _poss;

        public POSV2? POSV2 => _poss;

        public string CountryCode { get; set; } = string.Empty;

        public Api.PosSystem.Core.Interfaces.IMiddlewareClient MiddlewareClient => MiddlewareClientAndroid.FromV2(_poss!, CountryCode);

        public PackageConfiguration? QueueConfiguration { get; private set; }

        public MiddlewareV2Provider(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, bool isSandbox, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _loggerFactory = loggerFactory;
        }       

        public async Task StopAsync()
        {
            // MiddlewareProvider doesn't manage hosts directly, just helpers and wake locks
            //foreach (var helper in _helpers)
            //{
            //    helper.StopBegin();
            //    helper.StopEnd();
            //}
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
                var countryCode = Helpers.Configuration.GetQueueLocalization(queueConfiguration);
                var queueProvider = new SQLiteQueueProvider();
                _poss = queueProvider.CreatePOSV2(queueConfiguration, countryCode, _loggerFactory);
                Log.Logger.Information("Queue created. cashboxid: {cashbox.id}, queueId: {queue.id}", _cashboxId, queueConfiguration.Id);
                // Helipad is v1-only (IPOS-based) and has no V2/POSV2 adapter yet, so it isn't wired up here. that have to be add later. 
            }
            catch (Exception x)
            {
                Log.Logger.Error(x, "Failed to create worker. cashboxid: {cashbox.id}", _cashboxId);
                throw;
            }
        }
    }
}
