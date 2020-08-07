using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Helpers.Hosting;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Helper.Helipad;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common.Services.Helper
{
    public class HelipadHelperProvider
    {
        private const string HELIPAD_URL = "https://helipad-sandbox.fiskaltrust.cloud/";

        public IHelper CreateHelper(ftCashBoxConfiguration cashBoxConfiguration, string accessToken)
        {
            var config = new Dictionary<string, object>();
            config["cashboxid"] = cashBoxConfiguration.ftCashBoxId;
            config["accesstoken"] = accessToken;
            config["configuration"] = JsonConvert.SerializeObject(cashBoxConfiguration);
            config["server"] = HELIPAD_URL;

            var bootstrapper = new HelperBootstrapper
            {
                Configuration = config,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IClientFactory<IPOS>, POSClientFactory>();
            serviceCollection.AddLogCatLogging();

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IHelper>();
        }
    }
}