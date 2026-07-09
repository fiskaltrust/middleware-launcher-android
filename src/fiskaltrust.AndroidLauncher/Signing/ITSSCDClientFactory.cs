using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Soap;

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
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (_scus.TryGetValue(configuration.Url, out var scu))
            {
                return scu;
            }

            var retryPolicyoptions = new RetryPolicyOptions
            {
                DelayBetweenRetries = configuration.DelayBetweenRetries != default ? configuration.DelayBetweenRetries : RetryPolicyOptions.Default.DelayBetweenRetries,
                Retries = configuration.RetryCount ?? RetryPolicyOptions.Default.Retries,
                ClientTimeout = configuration.Timeout != default ? configuration.Timeout : RetryPolicyOptions.Default.ClientTimeout
            };

            return configuration.UrlType switch
            {
                "rest" => HttpITSSCDFactory.CreateSSCDAsync(new HttpITSSCDClientOptions
                {
                    Url = new Uri(configuration.Url.Replace("rest://", "http://")),
                    RetryPolicyOptions = retryPolicyoptions
                }).Result,
                "http" or "https" or "net.tcp" or "wcf" => SoapITSSCDFactory.CreateSSCDAsync(new ClientOptions
                {
                    Url = new Uri(configuration.Url),
                    RetryPolicyOptions = retryPolicyoptions
                }).Result,
                _ => throw new ArgumentException("This version of the fiskaltrust Launcher currently only supports REST and SOAP communication."),
            };
        }
    }
}
