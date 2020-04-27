using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;
using Newtonsoft.Json;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    class FiskalyScuProvider : IScuProvider
    {
        public async Task<IDESSCD> CreateScuAsync(Dictionary<string, object> scuConfiguration)
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(scuConfiguration["Configuration"]));
            return await Task.FromResult(new FiskalySCU(config));
        }
    }
}