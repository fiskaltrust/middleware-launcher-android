using Android.App;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Helper.Helipad;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Common.Services.Helper
{
    public class HelipadHelperProvider
    {
        public IHelper CreateHelper(ftCashBoxConfiguration cashBoxConfiguration, string accessToken, bool isSandbox, LogLevel logLevel, IEnumerable<IPOS> posHosts)
        {
            var config = new Dictionary<string, object>();
            config["cashboxid"] = cashBoxConfiguration.ftCashBoxId;
            config["accesstoken"] = accessToken;
            config["configuration"] = JsonConvert.SerializeObject(cashBoxConfiguration);
            config["server"] = isSandbox ? Urls.HELIPAD_SANDBOX : Urls.HELIPAD_PRODUCTION;
            config["sandbox"] = isSandbox;

            var bootstrapper = new HelperBootstrapper
            {
                Configuration = config,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IClientFactory<IPOS>>(new POSClientFactory(posHosts.First()));
            serviceCollection.AddLogProviders(logLevel);
            serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.Helper.Helipad", cashBoxConfiguration.ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IHelper>();
        }
    }
}