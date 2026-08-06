using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using fiskaltrust.Middleware.Interface.Client.Http.ATSSCD;
using fiskaltrust.Middleware.Interface.Client.Soap;

namespace fiskaltrust.AndroidLauncher.Signing
{
    public class ATSSCDClientFactory : IClientFactory<IATSSCD>
    {
        private readonly Dictionary<string, IATSSCD> _scus;
        private readonly Guid _cashboxId;
        private readonly string _accessToken;

        public ATSSCDClientFactory(Dictionary<string, IATSSCD> scus, Guid cashboxId, string accessToken)
        {
            _scus = scus;
            _cashboxId = cashboxId;
            _accessToken = accessToken;
        }

        public IATSSCD CreateClient(ClientConfiguration configuration)
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
                "rest" => HttpATSSCDFactory.CreateSSCDAsync(new HttpATSSCDClientOptions
                {
                    Url = new Uri(configuration.Url.Replace("rest://", "http://")),
                    RetryPolicyOptions = retryPolicyoptions
                }).Result,
                "https" => SoapATSSCDFactory.CreateSSCDAsync(new SoapClientOptions
                {
                    Url = new Uri(configuration.Url),
                    RetryPolicyOptions = retryPolicyoptions,
                    CashboxId = _cashboxId,
                    AccessToken = _accessToken
                }).Result,
                "http" or "net.tcp" or "wcf" => SoapATSSCDFactory.CreateSSCDAsync(new SoapClientOptions
                {
                    Url = new Uri(configuration.Url),
                    RetryPolicyOptions = retryPolicyoptions
                }).Result,
                _ => throw new ArgumentException("This version of the fiskaltrust Launcher currently only supports REST and SOAP communication."),
            };
        }
    }
}
