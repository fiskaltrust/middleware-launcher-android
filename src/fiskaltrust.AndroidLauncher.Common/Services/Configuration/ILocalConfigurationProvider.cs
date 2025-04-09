using fiskaltrust.storage.serialization.V0;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services.Configuration
{
    internal interface ILocalConfigurationProvider : IConfigurationProvider
    {
        Task PersistAsync(Guid cashboxId, string accessToken, ftCashBoxConfiguration configuration);
        Task<bool> ConfigurationExistsAsync(Guid cashboxId, string accessToken);
    }
}