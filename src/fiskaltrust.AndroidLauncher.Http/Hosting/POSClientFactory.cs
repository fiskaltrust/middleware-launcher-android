using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    internal class POSClientFactory : IClientFactory<IPOS>
    {
        private const int DEFAULT_TIMEOUT_SEC = 70;

        public IPOS CreateClient(ClientConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var retryPolicyoptions = new RetryPolicyOptions
            {
                ClientTimeout = configuration.Timeout == default ? TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SEC) : configuration.Timeout,
                DelayBetweenRetries = TimeSpan.FromSeconds(5),
                Retries = 2
            };

            return HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(configuration.Url.Replace("rest://", "http://")), RetryPolicyOptions = retryPolicyoptions }).Result;
        }
    }
}