using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    interface IDESSCDProvider
    {
        IDESSCD CreateSCU(string workingDir, PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel);
    }
}