using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Signing
{
    public class ITSSCDClientFactory : IClientFactory<IITSSCD>
    {
        private readonly Dictionary<string, IITSSCD> _scus;

        public ITSSCDClientFactory(Dictionary<string, IITSSCD> scus)
        {
            _scus = scus;
        }

        public IITSSCD CreateClient(ClientConfiguration configuration)
        {
            return _scus.First().Value;
        }
    }
}