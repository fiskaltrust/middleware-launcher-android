using fiskaltrust.AndroidLauncher;
using fiskaltrust.AndroidLauncher.Hosting;
//using fiskaltrust.AndroidLauncher.Http.Controllers;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<IPOS> CreatePosHost() => null;
    }
}