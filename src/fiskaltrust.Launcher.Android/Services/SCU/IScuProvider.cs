using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    interface IScuProvider
    {
        IDESSCD CreateScu(Dictionary<string, object> scuConfiguration);
    }
}