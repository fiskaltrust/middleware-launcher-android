using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using System;

namespace fiskaltrust.AndroidLauncher.Grpc.Hosting
{
    internal class DESSCDClientFactory : IClientFactory<IDESSCD>
    {
        private const int DEFAULT_TIMEOUT_SEC = 70;

        public IDESSCD CreateClient(ClientConfiguration configuration)
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

            return GrpcDESSCDFactory.CreateSSCDAsync(new GrpcClientOptions { Url = new Uri(configuration.Url), RetryPolicyOptions = retryPolicyoptions }).Result;
        }
    }
}