using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using fiskaltrust.Middleware.Interface.Client;
using System;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common.Signing
{
    public class ITSSCDClientFactory : IClientFactory<IITSSCD>
    {
        private const int DEFAULT_TIMEOUT_SEC = 70;
        private const int DEFAULT_DELAY_BETWEEN_RETRIES_SEC = 2;
        private const int DEFAULT_RETRIES = 2;

        private readonly Dictionary<string, IITSSCD> _scus;

        public ITSSCDClientFactory(Dictionary<string, IITSSCD> scus)
        {
            _scus = scus;
        }

        public IITSSCD CreateClient(ClientConfiguration configuration)
        {
            var options = new RetryPolicyOptions
            {
                ClientTimeout = configuration.Timeout == default ? TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SEC) : configuration.Timeout,
                DelayBetweenRetries = configuration.DelayBetweenRetries == default ? TimeSpan.FromSeconds(DEFAULT_DELAY_BETWEEN_RETRIES_SEC) : configuration.DelayBetweenRetries,
                Retries = configuration.RetryCount ?? DEFAULT_RETRIES
            };

            var retryPolicyHelper = new RetryPolicyHandler<IITSSCD>(options, new ProxyConnectionHandler<IITSSCD>(_scus[configuration.Url]));
            return new ITSSCDRetryProxyClient(retryPolicyHelper);
        }
    }
}