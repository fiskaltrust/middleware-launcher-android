using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<IDESSCD> CreateDeSscdHost() => new HttpDeSscdHost();

        public IHost<IPOS> CreatePosHost() => new HttpPosHost();
    }
}