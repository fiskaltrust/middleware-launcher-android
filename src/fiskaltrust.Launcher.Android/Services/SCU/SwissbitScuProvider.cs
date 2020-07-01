using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Support.V4.Content;
using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.AndroidLauncher.Services.SCU
{
    class SwissbitScuProvider : IScuProvider
    {
        public IDESSCD CreateSCU(Dictionary<string, object> scuConfiguration)
        {
            var dir = InitializeTseAsync();

            var scuConfig = new Dictionary<string, object>()
            {
                { "devicePath", dir }
            };

            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfig,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }

        private string InitializeTseAsync()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(Application.Context, null).Select(x => x.AbsolutePath);

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

            throw new RemountRequiredException("First call an uninitialized TSE; please either remount the SD card, or restart your device.");
        }
    }
}