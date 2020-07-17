using fiskaltrust.ifPOS.v1;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services
{
    public interface IPOSProvider
    {
        Task<IPOS> GetPOSAsync();
        Task StopAsync();
    }
}