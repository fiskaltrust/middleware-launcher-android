﻿using Android.App;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Helpers
{
    public static class Configuration
    {

        public static string GetAppInsightsInstrumentationKey(bool isSandbox)
        {
            var resourceId = isSandbox
                                ? Resource.String.app_insights_instrumentation_key_sandbox
                                : Resource.String.app_insights_instrumentation_key_production;

            return Application.Context.Resources.GetString(resourceId);
        }
    }
}
