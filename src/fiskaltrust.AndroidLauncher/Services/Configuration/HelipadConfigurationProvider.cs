using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    public class HelipadConfigurationProvider : IConfigurationProvider
    {
        private const string HELIPAD_URL = "https://helipad-sandbox.fiskaltrust.cloud/";

        public async Task<ftCashBoxConfiguration> GetCashboxConfigurationAsync(Guid cashboxId, string accessToken)
        {
            using(var httpClient = new HttpClient { BaseAddress = new Uri(HELIPAD_URL) })
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