using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    interface IScuProvider
    {
        IDESSCD CreateSCU(PackageConfiguration scuConfiguration, LogLevel logLevel);
    }
}