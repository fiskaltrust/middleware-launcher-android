using fiskaltrust.AndroidLauncher.Http.Controllers;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpPosHost : HttpHost<IPOS, POSController>
    {
        public override IClientFactory<IPOS> GetClientFactory() => new POSClientFactory();

        public override async Task<IPOS> GetProxyAsync()
        {
            return await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
            {
                Url = new Uri(Url)
            });
        }
    }
}