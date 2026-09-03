using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    public class HelipadConfigurationProvider : IConfigurationProvider
    {
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
            using var httpClient = new HttpClient { BaseAddress = new Uri(helipadUrl) };
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
