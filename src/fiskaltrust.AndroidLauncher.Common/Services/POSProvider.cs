using fiskaltrust.ifPOS.v1;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    class POSProvider : IPOSProvider
    {
        private MiddlewareLauncher _launcher;
        private IPOS _pos;

        public POSProvider(Guid cashboxId, string accessToken)
        {
            _launcher = new MiddlewareLauncher(cashboxId, accessToken);
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

        public async Task StopAsync() => await _launcher.StopAsync();
    }
}