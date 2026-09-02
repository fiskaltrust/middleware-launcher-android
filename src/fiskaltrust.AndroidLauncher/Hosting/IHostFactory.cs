using fiskaltrust.AndroidLauncher.Hosting;
using fiskaltrust.AndroidLauncher.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.AndroidLauncher
{
    public interface IHostFactory
    {
        IHost<IPOS> CreatePosHost();
    }
}