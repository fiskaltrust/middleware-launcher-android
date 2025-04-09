using System;
using System.IO;
using System.Linq;
using Android.App;
using AndroidX.Core.Content;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Swissbit;

// using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    class DESwissbitScuProvider : IDESSCDProvider
    {
        public IDESSCD CreateSCU(string workingDir, PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel)
        {
            var dir = InitializeTseAsync();
            scuConfiguration.Configuration["servicefolder"] = workingDir;
            scuConfiguration.Configuration["devicePath"] = dir;

            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            // serviceCollection.AddLogProviders(logLevel);
            // serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.SCU.DE.Swissbit", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }

        private string InitializeTseAsync()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(Android.App.Application.Context, null).Select(x => x.AbsolutePath);

            foreach (var dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, "TSE_INFO.DAT")))
                {
                    return dir;
                }

                var triggerFile = Path.Combine(dir, ".SwissbitWorm");
                if (File.Exists(triggerFile))
                    File.Delete(triggerFile);

                File.Create(triggerFile).Dispose();
            }

            throw new RemountRequiredException("First call to an uninitialized TSE; please either remount the SD card, or restart your device.");
        }
    }
}