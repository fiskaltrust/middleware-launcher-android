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
        public IHost<IPOS> CreatePosHost() => new HttpPosHost();
    }
}