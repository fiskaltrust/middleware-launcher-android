using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common.Signing
{
    public class DESSCDClientFactory : IClientFactory<IDESSCD>
    {
        private readonly Dictionary<string, IDESSCD> _scus;

        public DESSCDClientFactory(Dictionary<string, IDESSCD> scus)
        {            
            _scus = scus;
        }

        public IDESSCD CreateClient(ClientConfiguration configuration)
        {
            return _scus[configuration.Url];
        }
    }
}