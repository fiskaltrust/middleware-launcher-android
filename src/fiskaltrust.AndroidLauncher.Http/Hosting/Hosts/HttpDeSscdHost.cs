using fiskaltrust.AndroidLauncher.Http.Controllers;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpDeSscdHost : HttpHost<IDESSCD, DESSCDController>
    {
        public override IClientFactory<IDESSCD> GetClientFactory() => new DESSCDClientFactory();

        public override async Task<IDESSCD> GetProxyAsync()
        {
            return await HttpDESSCDFactory.CreateSSCDAsync(new HttpDESSCDClientOptions
            {
                Url = new Uri(Url)
            });
        }
    }
}