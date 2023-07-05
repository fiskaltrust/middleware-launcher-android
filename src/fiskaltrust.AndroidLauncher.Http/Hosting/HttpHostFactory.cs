using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<SSCD> CreateSscdHost<T>() where T : SSCD => typeof(T) switch
        {
            Type t when t == typeof(DESSCD) => (IHost<SSCD>)new HttpDeSscdHost(),
            Type t when t == typeof(ITSSCD) => (IHost<SSCD>)new HttpItSscdHost()
        };

        public IHost<IPOS> CreatePosHost() => new HttpPosHost();
    }
}