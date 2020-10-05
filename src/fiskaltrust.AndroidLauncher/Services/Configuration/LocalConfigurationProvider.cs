using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal class LocalConfigurationProvider : ILocalConfigurationProvider
    {
        private const string SETTING_KEY_PREFIX = "fiskaltrust_cashbox_";

        public async Task<ftCashBoxConfiguration> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            var value = await SecureStorage.GetAsync($"{SETTING_KEY_PREFIX}{cashboxId}");
            if (value == null)
                throw new ConfigurationNotFoundException($"The configuration for the cashbox {cashboxId} could not be downloaded. An internet connection is required at least on the initialization appempt of a cashbox.");

            return JsonConvert.DeserializeObject<ftCashBoxConfiguration>(value);
        }

        public async Task PersistAsync(Guid cashboxId, ftCashBoxConfiguration configuration)
        {
            await SecureStorage.SetAsync($"{SETTING_KEY_PREFIX}{cashboxId}", JsonConvert.SerializeObject(configuration));
        }
    }
}