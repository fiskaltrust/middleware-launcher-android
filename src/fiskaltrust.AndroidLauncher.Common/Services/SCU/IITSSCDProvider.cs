using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    interface IITSSCDProvider
    {
        IITSSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel);
    }
}