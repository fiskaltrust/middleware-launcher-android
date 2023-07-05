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
        SSCD CreateSCU(PackageConfiguration scuConfiguration, Guid ftCashBoxId, bool isSandbox, LogLevel logLevel);
    }

    public record SSCD {
        public object Instance { get; protected set; }
    }

    public interface ISSCD<T> {
        public T Instance { get; }
    }

    public record DESSCD : SSCD, ISSCD<IDESSCD> {
        public DESSCD(IDESSCD instance) => base.Instance = instance;

        public new IDESSCD Instance { get => (IDESSCD)base.Instance; }
    };

    public record ITSSCD : SSCD, ISSCD<IITSSCD>
    {
        public ITSSCD(IITSSCD instance) => base.Instance = instance;

        public new IITSSCD Instance { get => (IITSSCD)base.Instance; }
    };

    public class SSCDClientFactory<T> : IClientFactory<T>
    {
        private readonly IClientFactory<ISSCD<T>> _clientFactory;

        public SSCDClientFactory(IClientFactory<ISSCD<T>> clientFactory) => _clientFactory = clientFactory;

        public T CreateClient(ClientConfiguration configuration)
        {
            return _clientFactory.CreateClient(configuration).Instance;
        }
    }

    public static class IHostExt {
        public static IClientFactory<T> GetClientFactoryForSSCD<T>(this IHost<SSCD> host)
            => new SSCDClientFactory<T>(((IHost<ISSCD<T>>)host).GetClientFactory());
    }
}