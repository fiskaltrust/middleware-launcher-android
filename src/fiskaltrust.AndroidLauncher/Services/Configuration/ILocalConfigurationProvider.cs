﻿using fiskaltrust.storage.serialization.V0;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    internal interface ILocalConfigurationProvider : IConfigurationProvider
    {
        Task PersistAsync(Guid cashboxId, ftCashBoxConfiguration configuration);
    }
}