using fiskaltrust.storage.serialization.V0;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal interface IConfigurationProvider
    {
        Task<(ftCashBoxConfiguration configuration, bool isSandbox)> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken);
    }
}
