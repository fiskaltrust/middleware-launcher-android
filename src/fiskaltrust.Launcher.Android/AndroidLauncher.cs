using fiskaltrust.ifPOS.v1;
using fiskaltrust.Launcher.Android.Services.Configuration;
using fiskaltrust.Launcher.Android.Services.Queue;
using fiskaltrust.Launcher.Android.Services.SCU;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using Java.Lang;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            InitializeScu(configuration);
            InitializeQueue(configuration);
        }

        public async Task StartFiskalyDemoAsync()
        {
            var configuration = await _configurationProvider.GetCashboxConfigurationAsync(_cashboxId);

            InitializeFiskalyScu(configuration);
            InitializeQueue(configuration);
        }

        public async Task<IPOS> GetPOS()
        {
            return await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions
            {
                Url = new Uri("localhost:10300")
            });
        }

        private void InitializeScu(Dictionary<string, object> configuration)
        {
            var scus = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftSignaturCreationDevices"]));
            var scuConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(scus[0]));
            var url = (scuConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();

            var scuProvider = new SwissbitScuProvider();
            var scu = scuProvider.CreateSCU(scuConfiguration);
            _scuHost.StartService(url, scu);
        }

        private void InitializeFiskalyScu(Dictionary<string, object> configuration)
        {
            var scus = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftSignaturCreationDevices"]));
            var scuConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(scus[0]));
            var url = (scuConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();

            var scuProvider = new FiskalyScuProvider();
            var scu = scuProvider.CreateSCU(scuConfiguration);
            _scuHost.StartService(url, scu);
        }

        private void InitializeQueue(Dictionary<string, object> configuration)
        {
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            var url = (queueConfiguration["Url"] as Newtonsoft.Json.Linq.JArray)[0].ToString();

            var queueProvider = new QueueProvider();
            var pos = queueProvider.CreatePOS(Environment.GetFolderPath(Environment.SpecialFolder.Personal), queueConfiguration);

            _posHost.StartService(url, pos);
        }
    }
}