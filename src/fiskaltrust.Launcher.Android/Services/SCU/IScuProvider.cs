using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    interface IScuProvider
    {
        public IDESSCD CreateScu(Dictionary<string, object> scuConfiguration)
        {
            return new FiskalySCU(scuConfiguration);
        }
    }
}