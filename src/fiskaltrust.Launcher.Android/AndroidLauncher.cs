using fiskaltrust.ifPOS.v1;
using fiskaltrust.Launcher.Android.Services.Configuration;
using fiskaltrust.Launcher.Android.Services.SCU;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;
using fiskaltrust.Middleware.SCU.DE.Swissbit;
using Java.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace fiskaltrust.Launcher.Android
{
    public class AndroidLauncher
    {
        private readonly Guid _cashboxId;
        private IConfigurationProvider _configurationProvider;
        private readonly GrpcHost _posHost;
        private readonly GrpcHost _scuHost;

        public AndroidLauncher(Guid cashboxId)
        {
            _cashboxId = cashboxId;

            // TODO: Get cashbox config from helipad or package server, based on cashbox ID. For now, just load the attached one.
            _configurationProvider = new StaticConfigurationProvider();
            _posHost = new GrpcHost();
            _scuHost = new GrpcHost();
        }

        public async Task StartAsync()
        {
            var configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId);

            await InitializeScu(configuration);
            await InitializeQueueAsync(configuration);
        }

        public IPOS GetPOS()
        {
            return GrpcHelper.GetClient<IPOS>("localhost:10300");
        }

        private async Task InitializeScu(Dictionary<string, object> configuration)
        {
            var scus = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftSignaturCreationDevices"]));
            var scuConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(scus[0]));
            var url = (scuConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();

            var scuProvider = new FiskalyScuProvider();
            var scu = scuProvider.CreateScu(scuConfiguration);
            var info = await scu.GetTseInfoAsync();
            _scuHost.StartService(url, scu);
        }

        private async Task InitializeQueueAsync(Dictionary<string, object> configuration)
        {
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            var url = (queueConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();
            
            var queueProvider = new QueueProvider();
            var pos = await queueProvider.CreatePosAsync(Environment.GetFolderPath(Environment.SpecialFolder.Personal), queueConfiguration);

            _posHost.StartService(url, pos);
        }
    }
}