using Android.App;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Helpers
{
    public static class Configuration
    {
        private static readonly string APP_INSIGHTS_INSTRUMENTATION_KEY_ENV_VARIABLE_NAME = "APP_INSIGHTS_INSTRUMENTATION_KEY";

        public static string GetAppInsightsInstrumentationKey(bool isSandbox)
        {
            var fromEnv = Environment.GetEnvironmentVariable(APP_INSIGHTS_INSTRUMENTATION_KEY_ENV_VARIABLE_NAME);
            
            if(!string.IsNullOrEmpty(fromEnv))
            {
                return fromEnv;
            }

            var resourceId = isSandbox
                                ? Resource.String.app_insights_instrumentation_key_sandbox
                                : Resource.String.app_insights_instrumentation_key_production;

            return Application.Context.Resources.GetString(resourceId);
        }
    }
}
