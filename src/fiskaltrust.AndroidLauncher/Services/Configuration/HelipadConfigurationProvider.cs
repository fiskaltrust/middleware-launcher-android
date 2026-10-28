using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    public class HelipadConfigurationProvider : IConfigurationProvider
    {
        public async Task<(ftCashBoxConfiguration configuration, string configurationJson, bool isSandbox)> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            {
                var productionResult = await GetConfigurationAsync(cashboxId, accessToken, false);
                if (productionResult.IsSuccessStatusCode)
                {
                    var content = await productionResult.Content.ReadAsStringAsync();
                    return (await ParseResponse(content), content, false);
                }
            }

            {
                var sandboxResult = await GetConfigurationAsync(cashboxId, accessToken, true);
                sandboxResult.EnsureSuccessStatusCode();
                var content = await sandboxResult.Content.ReadAsStringAsync();

                return (await ParseResponse(content), content, true);
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

        private async Task<ftCashBoxConfiguration> ParseResponse(string content)
        {
            return JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
        }
    }
}
