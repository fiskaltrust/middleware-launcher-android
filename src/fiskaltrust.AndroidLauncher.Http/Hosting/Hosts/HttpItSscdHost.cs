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
    public class HttpItSscdHost : HttpHost<IITSSCD, ITSSCDController>
    {
        public override IClientFactory<IITSSCD> GetClientFactory() => new ITSSCDClientFactory();

        public override async Task<IITSSCD> GetProxyAsync()
        {
            return await HttpITSSCDFactory.CreateSSCDAsync(new HttpITSSCDClientOptions
            {
                Url = new Uri(Url)
            });
        }
    }
}