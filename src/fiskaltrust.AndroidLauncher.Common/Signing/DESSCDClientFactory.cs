using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client.Common.RetryLogic;
using fiskaltrust.Middleware.Interface.Client;
using System;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common.Signing
{
    public class DESSCDClientFactory : IClientFactory<IDESSCD>
    {
        private const int DEFAULT_TIMEOUT_SEC = 70;
        private const int DEFAULT_DELAY_BETWEEN_RETRIES_SEC = 2;
        private const int DEFAULT_RETRIES = 2;

        private readonly Dictionary<string, IDESSCD> _scus;

        public DESSCDClientFactory(Dictionary<string, IDESSCD> scus)
        {            
            _scus = scus;
        }

        public IDESSCD CreateClient(ClientConfiguration configuration)
        {
            var options = new RetryPolicyOptions
            {
                ClientTimeout = configuration.Timeout == default ? TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SEC) : configuration.Timeout,
                DelayBetweenRetries = configuration.DelayBetweenRetries == default ? TimeSpan.FromSeconds(DEFAULT_DELAY_BETWEEN_RETRIES_SEC) : configuration.DelayBetweenRetries,
                Retries = configuration.RetryCount ?? DEFAULT_RETRIES
            };

            var retryPolicyHelper = new RetryPolicyHandler<IDESSCD>(options, new ProxyConnectionHandler<IDESSCD>(_scus[configuration.Url]));
            return new DESSCDRetryProxyClient(retryPolicyHelper);
        }
    }
}