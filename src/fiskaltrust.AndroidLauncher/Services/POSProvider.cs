using fiskaltrust.ifPOS.v1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services
{
    class POSProvider : IPOSProvider
    {
        private MiddlewareLauncher _launcher;
        private IPOS _pos;

        public POSProvider(Guid cashboxId, string accessToken, bool isSandbox, Dictionary<string, object> scuParams)
        {
            _launcher = new MiddlewareLauncher(cashboxId, accessToken, isSandbox, scuParams);
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