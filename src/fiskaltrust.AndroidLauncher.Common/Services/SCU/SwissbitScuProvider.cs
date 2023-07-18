using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using Android.App;
using Android.Hardware.Usb;
using Android.Support.V4.Content;
using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{

    class SwissbitScuProvider : IScuProvider
    {
        public IDESSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel)
        {
            var dir = InitializeTseAsync();
            scuConfiguration.Configuration["devicePath"] = dir;

            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogProviders(logLevel);
            serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.SCU.DE.Swissbit", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }

        private string InitializeTseAsync()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(Application.Context, null).Select(x => x.AbsolutePath);

            foreach (var dir in dirs)
            {
                Log.Logger.Information($"Directory: '{dir}'.");

                if (File.Exists(Path.Combine(dir, "TSE_INFO.DAT")))
                {
                    return dir;
                }

                var triggerFile = Path.Combine(dir, ".SwissbitWorm");
                if (File.Exists(triggerFile))
                    File.Delete(triggerFile);

                File.Create(triggerFile).Dispose();
            }
            UsbManager manager = (UsbManager)Application.Context.GetSystemService("usb");
            var deviceList = manager.DeviceList.Keys;
            if (deviceList.Count == 0)
                Log.Logger.Information($"No USB dir found");

            foreach (var dir in deviceList)
            {
                Log.Logger.Information($"Directory usb: '{dir}'.");

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