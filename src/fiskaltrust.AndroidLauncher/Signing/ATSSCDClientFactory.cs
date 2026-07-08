using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
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
            try
            {
                return _scus[configuration.Url];
            }
            catch
            {
                if (configuration is null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                var retryPolicyoptions = new RetryPolicyOptions
                {
                    DelayBetweenRetries = configuration.DelayBetweenRetries != default ? configuration.DelayBetweenRetries : RetryPolicyOptions.Default.DelayBetweenRetries,
                    Retries = configuration.RetryCount ?? RetryPolicyOptions.Default.Retries,
                    ClientTimeout = configuration.Timeout != default ? configuration.Timeout : RetryPolicyOptions.Default.ClientTimeout
                };

                var isHttps = !string.IsNullOrEmpty(_launcherConfiguration.TlsCertificatePath) || !string.IsNullOrEmpty(_launcherConfiguration.TlsCertificateBase64);
                var sslValidationDisabled = _launcherConfiguration.SslValidation!.Value;

                return configuration.UrlType switch
                {
                    "grpc" => GrpcATSSCDFactory.CreateSSCDAsync(new GrpcClientOptions
                    {
                        Url = new Uri(configuration.Url.Replace("grpc://", isHttps ? "https://" : "http://")),
                        RetryPolicyOptions = retryPolicyoptions,
                        ChannelOptions = new GrpcChannelOptions
                        {
                            Credentials = isHttps ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure,
                            HttpHandler = isHttps && sslValidationDisabled ? new HttpClientHandler { ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true } : null
                        }
                    }).Result,
                    "rest" => HttpATSSCDFactory.CreateSSCDAsync(new HttpATSSCDClientOptions
                    {
                        Url = new Uri(configuration.Url.Replace("rest://", isHttps ? "https://" : "http://")),
                        RetryPolicyOptions = retryPolicyoptions,
                        DisableSslValidation = sslValidationDisabled
                    }).Result,
                    "https" => SoapATSSCDFactory.CreateSSCDAsync(new SoapClientOptions
                    {
                        Url = new Uri(configuration.Url),
                        RetryPolicyOptions = retryPolicyoptions,
                        CashboxId = _launcherConfiguration.CashboxId!.Value,
                        AccessToken = _launcherConfiguration.AccessToken
                    }).Result,
                    "http" or "https" or "net.tcp" or "wcf" => SoapATSSCDFactory.CreateSSCDAsync(new SoapClientOptions
                    {
                        Url = new Uri(configuration.Url),
                        RetryPolicyOptions = retryPolicyoptions
                    }).Result,
                    _ => throw new ArgumentException("This version of the fiskaltrust Launcher currently only supports gRPC, REST and SOAP communication."),
                };
            }
        }
    }
}