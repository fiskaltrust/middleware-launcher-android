using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Services.SCU
{
    interface IATSSCDProvider
    {
        IATSSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel);
    }
}
