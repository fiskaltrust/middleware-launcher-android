using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    public class HelipadConfigurationProvider : IConfigurationProvider
    {
        // The configuration is fetched on every middleware start, before the first receipt is answered, and the
        // stored configuration is the fallback (ConfigurationProvider). Without a limit, a network that drops
        // packets (a store firewall, a captive portal) held the POS for the HttpClient default of 100 s per call,
        // production and sandbox, before that fallback was used.
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

        public async Task<(ftCashBoxConfiguration configuration, bool isSandbox)> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            {
                var productionResult = await GetConfigurationAsync(cashboxId, accessToken, false);
                if (productionResult.IsSuccessStatusCode)
                {
                    return (await ParseResponse(productionResult), false);
                }
            }

            {
                var sandboxResult = await GetConfigurationAsync(cashboxId, accessToken, true);
                sandboxResult.EnsureSuccessStatusCode();
                return (await ParseResponse(sandboxResult), true);
            }
        }

        private async Task<HttpResponseMessage> GetConfigurationAsync(Guid cashboxId, string accessToken, bool isSandbox)
        {
            var helipadUrl = isSandbox ? Urls.HELIPAD_SANDBOX : Urls.HELIPAD_PRODUCTION;
            using var httpClient = new HttpClient { BaseAddress = new Uri(helipadUrl), Timeout = RequestTimeout };
            httpClient.DefaultRequestHeaders.Add("cashboxid", cashboxId.ToString());
            httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);

            var result = await httpClient.GetAsync("api/Configuration");
            return result;
        }

        private async Task<ftCashBoxConfiguration> ParseResponse(HttpResponseMessage result)
        {
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
        }
    }
}
