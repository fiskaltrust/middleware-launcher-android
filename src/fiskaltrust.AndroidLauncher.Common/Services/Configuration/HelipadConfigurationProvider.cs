using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Services.Configuration
{
    public class HelipadConfigurationProvider : IConfigurationProvider
    {
        public async Task<ftCashBoxConfiguration> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken, bool isSandbox)
        {
            var helipadUrl = isSandbox ? Urls.HELIPAD_SANDBOX : Urls.HELIPAD_PRODUCTION;
            using(var httpClient = new HttpClient { BaseAddress = new Uri(helipadUrl) })
            {
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashboxId.ToString());
                httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
                
                var result = await httpClient.GetAsync("api/Configuration");
                result.EnsureSuccessStatusCode();

                var content = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
            }
        }
    }
}