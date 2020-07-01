using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal interface IConfigurationProvider
    {
        Task<Dictionary<string, object>> GetCashboxConfigurationAsync(Guid cashboxId);
    }
}