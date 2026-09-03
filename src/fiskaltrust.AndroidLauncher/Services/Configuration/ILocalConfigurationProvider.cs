using fiskaltrust.storage.serialization.V0;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal interface ILocalConfigurationProvider : IConfigurationProvider
    {
        Task PersistAsync(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration, bool isSandbox);
        Task<bool> ConfigurationExistsAsync(Guid cashboxId, string accessToken);
    }
}
