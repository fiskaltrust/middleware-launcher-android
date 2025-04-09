using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.CustomRTServer;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    public class ITCustomRTServerScuProvider : IITSSCDProvider
    {
        public IITSSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel)
        {
            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            // serviceCollection.AddLogProviders(logLevel);
            // serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.SCU.IT.CustomRTServer", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }
    }
}