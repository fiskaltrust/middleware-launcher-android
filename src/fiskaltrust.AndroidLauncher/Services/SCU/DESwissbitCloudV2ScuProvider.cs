using Android.App;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Services.SCU
{
    class DESwissbitCloudV2ScuProvider : IDESSCDProvider
    {
        public IDESSCD CreateSCU(string workingDir, PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel)
        {
            scuConfiguration.Configuration["servicefolder"] = workingDir;
            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogProviders(logLevel);
            serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }
    }
}
