using Azure.Core;
using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.AndroidLauncher.Services.Helper;
using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.AndroidLauncher.Services.Queue;
using fiskaltrust.Api.PosSystem.Core.Payment;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.SCU.PL.InMemory;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.SQLite;
using fiskaltrust.storage.serialization.V0;
using Java.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System.Security.Cryptography;
using System.Text;

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
        private readonly LogLevel _logLevel;
        private readonly ILoggerFactory _loggerFactory;


        private List<IHelper> _helpers;
        private POSV2? _poss;

        public POSV2? POSV2 => _poss;

        public string CountryCode { get; set; } = string.Empty;

        public Api.PosSystem.Core.Interfaces.IMiddlewareClient MiddlewareClient => MiddlewareClientAndroid.FromV2(_poss!, CountryCode);

        public PackageConfiguration? QueueConfiguration { get; private set; }

        public MiddlewareV2Provider(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, bool isSandbox, ILoggerFactory loggerFactory, LogLevel logLevel)
        {
            _configuration = configuration;
            _cashboxId = cashboxId;
            _accessToken = accessToken;
            _isSandbox = isSandbox;
            _loggerFactory = loggerFactory;
            _logLevel = logLevel;

            _helpers = new List<IHelper>();

        }

        //public async Task StartAsync()
        //{
        //    if (_configuration.ftQueues.Count() != 1)
        //    {
        //        throw new ArgumentException("The Android launcher currently only supports exactly one queue package.");
        //    }

        //    foreach (var queueConfig in _configuration.ftQueues)
        //    {
        //        queueConfig.Configuration["sandbox"] = _isSandbox;
        //        await InitializeQueueAsync(queueConfig);
        //    }

        //    //await InitializeHelipadHelperAsync(_configuration);
        //}

        public async Task StopAsync()
        {
            // MiddlewareProvider doesn't manage hosts directly, just helpers and wake locks
            foreach (var helper in _helpers)
            {
                helper.StopBegin();
                helper.StopEnd();
            }
        }

        //private async Task InitializeQueueAsync(PackageConfiguration packageConfig)
        //{
        //    var queueProvider = new SQLiteQueueProvider();
        //    var pos = await Task.Run(() => queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), packageConfig, _cashboxId, _accessToken, _isSandbox, _logLevel, _scus));
        //    _poss = pos;
        //    var queues = ParseParameter<List<ftQueue>>(packageConfig.Configuration, "init_ftQueue") ?? new List<ftQueue>();
        //    QueueConfiguration = packageConfig;
        //    CountryCode = queues.FirstOrDefault()?.CountryCode?.ToUpper();
        //    Log.Logger.Debug($"REST endpoint for type 'fiskaltrust.Middleware.Queue.SQLite' is listening on 'Intnet Interface'.");
        //}
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
            //using var activity = System.Diagnostics.Activity.Current?.Source.StartActivity("cashbox.initialize");
            //activity?.AddTag("cashbox.configuration.timestamp", _configuration.TimeStamp);
            //activity?.AddTag("cashbox.configuration.hash", SHA256.HashData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_configuration))));

            try
            {
                var queueConfiguration = _configuration.ftQueues[0];
                var countryCode = Helpers.Configuration.GetQueueLocalization(queueConfiguration);
                var queueProvider = new SQLiteQueueProvider();
                _poss = queueProvider.CreatePOSV2(queueConfiguration, countryCode, _loggerFactory);
                Log.Logger.Information("Worker created. cashboxid: {cashbox.id}", _cashboxId);
                // Helipad billing is v1-only (IPOS-based) and has no V2/POSV2 adapter yet, so it isn't wired up here.
            }
            catch (Exception x)
            {
                Log.Logger.Error(x, "Failed to create worker. cashboxid: {cashbox.id}", _cashboxId);
                throw;
            }
        }
    }
}
