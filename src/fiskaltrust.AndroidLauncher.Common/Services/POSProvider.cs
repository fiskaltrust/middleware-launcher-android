using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services
{
    class POSProvider : IPOSProvider
    {
        private MiddlewareLauncher _launcher;
        private IPOS _pos;

        public POSProvider(Guid cashboxId, string accessToken, bool isSandbox, LogLevel logLevel, Dictionary<string, object> scuParams)
        {
            _launcher = new MiddlewareLauncher(cashboxId, accessToken, isSandbox, logLevel, scuParams);
        }

        public async Task<IPOS> GetPOSAsync()
        {
            if (_pos == null)
            {
                try
                {
                    await AdminEndpointService.Instance.StartAsync();
                }
                catch (Exception) { }

                await _launcher.StartAsync();
                _pos = await _launcher.GetPOS();
            }

            return _pos;
        }

        public async Task StopAsync()
        {
            await AdminEndpointService.Instance.StopAsync();
            await _launcher.StopAsync();
        }
    }
}