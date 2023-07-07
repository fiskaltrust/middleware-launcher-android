using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Runtime.CompilerServices;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    interface IScuProvider
    {
        T CreateSCU<T>(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel);
    }
}