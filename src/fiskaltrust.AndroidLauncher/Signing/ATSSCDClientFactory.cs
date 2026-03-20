using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Signing
{
    public class ATSSCDClientFactory : IClientFactory<IATSSCD>
    {
        private readonly Dictionary<string, IATSSCD> _scus;

        public ATSSCDClientFactory(Dictionary<string, IATSSCD> scus)
        {
            _scus = scus;
        }

        public IATSSCD CreateClient(ClientConfiguration configuration)
        {
            return _scus[configuration.Url];
        }
    }
}
