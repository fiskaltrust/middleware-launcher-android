using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.AndroidLauncher.Http.Controllers;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpItSscdHost : HttpHost<ITSSCD, ITSSCDController>
    {
        public override IClientFactory<ITSSCD> GetClientFactory() => new ITSSCDClientFactory();

        public override async Task<ITSSCD> GetProxyAsync()
        {
            return new ITSSCD(await HttpITSSCDFactory.CreateSSCDAsync(new HttpITSSCDClientOptions
            {
                Url = new Uri(Url)
            }));
        }
    }
}