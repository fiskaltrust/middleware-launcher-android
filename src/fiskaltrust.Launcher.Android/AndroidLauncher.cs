using fiskaltrust.ifPOS.v1;
using fiskaltrust.Launcher.Android.Services.Configuration;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;
using Java.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.Launcher.Android
{
    public class AndroidLauncher
    {
        private readonly Guid _cashboxId;
        private IConfigurationProvider _configurationProvider;
        private readonly GrpcHost _grpcHost;

        public AndroidLauncher(Guid cashboxId)
        {
            _cashboxId = cashboxId;

            // TODO: Get cashbox config from helipad or package server, based on cashbox ID. For now, just load the attached one.
            _configurationProvider = new StaticConfigurationProvider();
            _grpcHost = new GrpcHost();
        }

        public async Task StartAsync()
        {
            var configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId);

            await InitializeScuAsync(configuration);
            await InitializeQueueAsync(configuration);
        }

        public IPOS GetPOS()
        {
            return GrpcHelper.GetClient<IPOS>("localhost:10300");
        }

        private async Task InitializeScuAsync(Dictionary<string, object> configuration)
        {
            var scuConfig = new Dictionary<string, object>()
            {
                { "devicePath", "T:" }
            };

            using (var scu = new SwissbitSCU(scuConfig))
            {
                scu.WaitForInitialization().Wait();
                var tseInfo = scu.GetTseInfoAsync().Result;
            }

            await Task.CompletedTask;
        }

        private async Task InitializeQueueAsync(Dictionary<string, object> configuration)
        {
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            var url = (queueConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();
            
            var queueProvider = new QueueProvider();
            var pos = await queueProvider.CreatePos(Environment.GetFolderPath(Environment.SpecialFolder.Personal), queueConfiguration);

            _grpcHost.StartService(url, pos);
        }
    }
}