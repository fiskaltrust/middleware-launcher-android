using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Support.V4.Content;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Launcher.Android.Exceptions;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    class SwissbitScuProvider : IScuProvider
    {
        public async Task<IDESSCD> CreateScuAsync(Dictionary<string, object> scuConfiguration)
        {        
            var dir = InitializeTseAsync();

            var scuConfig = new Dictionary<string, object>()
            {
                { "devicePath", dir }
            };
            var scu = new SwissbitSCU(scuConfig);
            await scu.WaitForInitializationAsync();
            
            return scu;
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