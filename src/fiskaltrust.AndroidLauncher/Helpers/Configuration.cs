using Android.App;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using System;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public static class Configuration
    {

        public static string GetAppInsightsInstrumentationKey(bool isSandbox)
        {
            var resourceId = isSandbox
                                ? Resource.String.app_insights_instrumentation_key_sandbox
                                : Resource.String.app_insights_instrumentation_key_production;

            return Android.App.Application.Context.Resources.GetString(resourceId);
        }

        public static string GetQueueLocalization(PackageConfiguration queueConfiguration)
        {
       
            var key = "init_ftQueue";
            if (queueConfiguration.Configuration.ContainsKey(key))
            {
                var queue = JsonConvert.DeserializeObject<List<ftQueue>>(queueConfiguration.Configuration[key].ToString());
                return queue.FirstOrDefault().CountryCode;
            }
            else
            {
                throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
            }
        }
    }
}
