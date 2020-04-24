using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Launcher.Android
{
    internal interface IConfigurationProvider
    {
        Task<Dictionary<string, object>> GetCashboxConfigurationAsync(Guid cashboxId);
    }
}