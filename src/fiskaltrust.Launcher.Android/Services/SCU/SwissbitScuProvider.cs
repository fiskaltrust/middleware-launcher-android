using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    class SwissbitScuProvider : IScuProvider
    {
        public async Task<IDESSCD> CreateScuAsync(Dictionary<string, object> scuConfiguration)
        {
            var scuConfig = new Dictionary<string, object>()
            {
                { "devicePath", "T:" }
            };
            var scu = new SwissbitSCU(scuConfig);
            await scu.WaitForInitialization();

            return scu;
        }
    }
}