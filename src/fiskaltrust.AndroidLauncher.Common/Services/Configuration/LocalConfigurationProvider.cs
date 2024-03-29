﻿using fiskaltrust.AndroidLauncher.Common.Exceptions;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace fiskaltrust.AndroidLauncher.Common.Services.Configuration
{
    internal class LocalConfigurationProvider : ILocalConfigurationProvider
    {
        private const string SETTING_USED = "fiskaltrust_cashbox_";
        private const string SETTING_CASHBOX_KEY_PREFIX = "fiskaltrust_cashbox_";
        private const string SETTING_CREDENTIALS_KEY_PREFIX = "fiskaltrust_credentials_";

        public async Task<bool> ConfigurationExistsAsync(Guid cashboxId, string accessToken)
        {
            return (await SecureStorage.GetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}")) != null;
        }

        public async Task<bool> IsConfigStoreEmptyAsync()
        {
            return (await SecureStorage.GetAsync(SETTING_USED)) == null;
        }

        public async Task<ftCashBoxConfiguration> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken, bool isSandbox)
        {
            var value = await SecureStorage.GetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}");
            if (value == null)
                throw new ConfigurationNotFoundException($"The configuration for the cashbox {cashboxId} could not be downloaded. An internet connection is required at least on the initialization appempt of a cashbox.");

            return JsonConvert.DeserializeObject<ftCashBoxConfiguration>(value);
        }

        public async Task PersistAsync(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration)
        {            
            await SecureStorage.SetAsync($"{SETTING_CASHBOX_KEY_PREFIX}{cashboxId}", JsonConvert.SerializeObject(configuration));
            await SecureStorage.SetAsync($"{SETTING_CREDENTIALS_KEY_PREFIX}{cashboxId}", accessToken);
            await SecureStorage.SetAsync(SETTING_USED, "true");
        }
    }
}