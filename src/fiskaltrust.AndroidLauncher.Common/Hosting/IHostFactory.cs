using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.AndroidLauncher.Common
{
    public interface IHostFactory
    {
        IHost<IPOS> CreatePosHost();
        IHost<SSCD> CreateSscdHost<T>() where T : SSCD;
    }
}