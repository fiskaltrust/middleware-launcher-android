using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    interface IScuProvider
    {
        IDESSCD CreateSCU(Dictionary<string, object> scuConfiguration);
    }
}