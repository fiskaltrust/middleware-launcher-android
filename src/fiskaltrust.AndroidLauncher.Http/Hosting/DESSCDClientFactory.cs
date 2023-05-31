using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    internal class DESSCDClientFactory : IClientFactory<IDESSCD>
    {
        private const int DEFAULT_TIMEOUT_SEC = 70;
        private const int DEFAULT_DELAY_BETWEEN_RETRIES_SEC = 2;
        private const int DEFAULT_RETRIES = 2;

        public IDESSCD CreateClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var retryPolicyoptions = new RetryPolicyOptions
            {
                ClientTimeout = configuration.Timeout == default ? TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SEC) : configuration.Timeout,
                DelayBetweenRetries = configuration.DelayBetweenRetries == default ? TimeSpan.FromSeconds(DEFAULT_DELAY_BETWEEN_RETRIES_SEC) : configuration.DelayBetweenRetries,
                Retries = configuration.RetryCount ?? DEFAULT_RETRIES
            };

            return HttpDESSCDFactory.CreateSSCDAsync(new HttpDESSCDClientOptions { Url = new Uri(configuration.Url.Replace("rest://", "http://")), RetryPolicyOptions = retryPolicyoptions }).Result;
        }
    }
}