using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Http.Controllers;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<IPOS> CreatePosHost() => new HttpHost<IPOS, POSController>();
    }
}