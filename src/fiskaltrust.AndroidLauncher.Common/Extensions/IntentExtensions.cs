using Android.Content;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
{
    public static class IntentExtensions
    {
        private const string SCU_CONFIG_PREFIX = "scu-config-";

        public static Dictionary<string, object> GetScuConfigParameters(this Intent intent, bool removePrefix = false)
        {
            string GetKey(string key) => removePrefix ? key.Substring(SCU_CONFIG_PREFIX.Length) : key;

            return intent.Extras.KeySet()
                .Where(x => x.StartsWith(SCU_CONFIG_PREFIX))
                .ToDictionary<string, string, object>(key => GetKey(key), key => intent.Extras.Get(key));
        }
    }
}