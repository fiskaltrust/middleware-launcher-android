using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    interface IScuProvider
    {
        Task<IDESSCD> CreateScuAsync(Dictionary<string, object> scuConfiguration);
    }
}