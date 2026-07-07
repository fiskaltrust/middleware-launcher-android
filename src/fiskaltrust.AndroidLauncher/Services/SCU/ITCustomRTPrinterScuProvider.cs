using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Services.SCU
{
    class ITCustomRTPrinterScuProvider : IITSSCDProvider
    {
        public IITSSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel)
        {
            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogProviders(logLevel);
            serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.SCU.IT.CustomRTPrinter", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }
    }
}
