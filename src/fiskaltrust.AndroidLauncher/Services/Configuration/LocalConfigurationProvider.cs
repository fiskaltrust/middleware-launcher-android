using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal class LocalConfigurationProvider : ILocalConfigurationProvider
    {
        private const string SETTING_USED = "fiskaltrust_cashbox_";
        private const string SETTING_CASHBOX_KEY_PREFIX = "fiskaltrust_cashbox_";
        private const string SETTING_SANDBOX_KEY_PREFIX = "fiskaltrust_cashbox_sandbox_";
        private const string SETTING_CREDENTIALS_KEY_PREFIX = "fiskaltrust_credentials_";

        public async Task<bool> ConfigurationExistsAsync(Guid cashboxId, string accessToken)
        {
            return (await SecureStorage.GetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}")) != null;
        }

        public async Task<bool> IsConfigStoreEmptyAsync()
        {
            return (await SecureStorage.GetAsync(SETTING_USED)) == null;
        }

        public async Task<(ftCashBoxConfiguration configuration, bool isSandbox)> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            var configuration = await SecureStorage.GetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}");
            var isSandboxString = await SecureStorage.GetAsync($"{SETTING_SANDBOX_KEY_PREFIX}{cashboxId}");

            if (configuration == null || !bool.TryParse(isSandboxString, out var isSandbox))
                throw new ConfigurationNotFoundException("Could not load config from local storage.");

            if (await SecureStorage.GetAsync($"{SETTING_CREDENTIALS_KEY_PREFIX}{cashboxId}") != accessToken)
            {
                throw new ConfigurationNotFoundException("Access token does not match the stored configuration.");
            }
            return (JsonConvert.DeserializeObject<ftCashBoxConfiguration>(configuration), isSandbox);
        }

        public async Task PersistAsync(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, bool isSandbox)
        {
            await SecureStorage.SetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}", JsonConvert.SerializeObject(configuration));
            await SecureStorage.SetAsync($"{SETTING_CREDENTIALS_KEY_PREFIX}{cashboxId}", accessToken);
            await SecureStorage.SetAsync($"{SETTING_SANDBOX_KEY_PREFIX}{cashboxId}", isSandbox.ToString());
            await SecureStorage.SetAsync(SETTING_USED, "true");
        }
    }
}
