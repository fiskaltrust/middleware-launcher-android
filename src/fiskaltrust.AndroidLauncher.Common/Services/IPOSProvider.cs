using fiskaltrust.ifPOS.v1;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    public interface IPOSProvider
    {
        Task<IPOS> GetPOSAsync();
        Task StopAsync();
    }
}