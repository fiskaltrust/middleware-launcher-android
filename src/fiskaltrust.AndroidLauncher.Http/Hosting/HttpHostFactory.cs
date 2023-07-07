using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.it;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<T> CreateSscdHost<T>() => typeof(T) switch
        {
            Type t when t == typeof(IDESSCD) => (IHost<T>)new HttpDeSscdHost(),
            Type t when t == typeof(IITSSCD) => (IHost<T>)new HttpItSscdHost()
        };

        public IHost<IPOS> CreatePosHost() => new HttpPosHost();
    }
}