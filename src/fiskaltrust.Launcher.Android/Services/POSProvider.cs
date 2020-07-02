using fiskaltrust.ifPOS.v1;
using Java.Lang;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services
{
    class POSProvider : IPOSProvider
    {
        private readonly MiddlewareLauncher _launcher;
        private IPOS _pos;

        public POSProvider()
        {
            _launcher = new MiddlewareLauncher(Guid.Empty);
        }

        public async Task<IPOS> GetPOSAsync()
        {
            if (_pos == null)
            {
                await _launcher.StartAsync();
                _pos = await _launcher.GetPOS();
            }

            return _pos;
        }
    }
}