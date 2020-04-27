using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitAndroid;

namespace fiskaltrust.Launcher.Android.Services.SCU
{
    class SwissbitScuProvider : IScuProvider
    {
        public IDESSCD CreateScu(Dictionary<string, object> scuConfiguration)
        {
            var scuConfig = new Dictionary<string, object>()
            {
                { "devicePath", "T:" }
            };
            return new SwissbitSCU(scuConfig);
        }
    }
}