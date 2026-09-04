using fiskaltrust.AndroidLauncher.Exceptions;
using fiskaltrust.storage.serialization.V0;
using Serilog;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal class ConfigurationProvider : IConfigurationProvider
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILocalConfigurationProvider _localConfigurationProvider;

        public ConfigurationProvider(IConfigurationProvider configurationProvider, ILocalConfigurationProvider localConfigurationProvider)
        {
            _configurationProvider = configurationProvider;
            _localConfigurationProvider = localConfigurationProvider;
        }

        public async Task<(ftCashBoxConfiguration configuration, bool isSandbox)> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            ftCashBoxConfiguration configuration;
            bool isSandbox;
            try
            {
                (configuration, isSandbox) = await _configurationProvider.GetCashboxConfigurationAsync(cashboxId, accessToken);
                await _localConfigurationProvider.PersistAsync(cashboxId, accessToken, configuration, isSandbox);
            }
            catch (Exception e)
            {
                try
                {
                    (configuration, isSandbox) = await _localConfigurationProvider.GetCashboxConfigurationAsync(cashboxId, accessToken);
                }
                catch
                {
                    Log.Logger.Error(e, "An error occured while downloading the configuration.");
                    throw new ConfigurationNotFoundException($"The configuration for the cashbox {cashboxId} could not be downloaded. An internet connection is required at least on the initialization attempt of a cashbox.", e);
                }
            }

            return (configuration, isSandbox);
        }
    }
}
