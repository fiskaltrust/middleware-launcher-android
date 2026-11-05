using fiskaltrust.AndroidLauncher.Constants;
using fiskaltrust.AndroidLauncher.Models;
using fiskaltrust.AndroidLauncher.Services.Helper.Exceptions;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public class HelipadService : IHelipadService
    {
        private readonly Uri _baseAddress;
        private readonly ILogger<HelipadService> _logger;

        // One client for the uploads: the service is a singleton and the uploader calls this path up to six times per
        // cashbox every 30 s, so a client per call would open and close a connection pool each time. Per-cashbox
        // headers go on the request, never on DefaultRequestHeaders, since the client is shared between cashboxes.
        private readonly HttpClient _uploadClient;

        public HelipadService(ILogger<HelipadService> logger, bool isSandbox)
        {
            _baseAddress = new Uri(isSandbox ? Urls.HELIPAD_SANDBOX : Urls.HELIPAD_PRODUCTION);
            _logger = logger;
            _uploadClient = new HttpClient { BaseAddress = _baseAddress, Timeout = TimeSpan.FromMinutes(5) };
        }
        public async Task UploadQueueListAsync(CashBoxCredentials cashBoxCredentials, List<dynamic> data)
        {
            var url = $"api/service/queue";
            foreach (var item in data)
            {
                try
                {
                    await UploadDataRequestAsync(cashBoxCredentials, url, item);
                }
                catch (HelipadException x)
                {
                    _logger.LogError(x, "Failed to execute UploadQueueListAsync for cashbox id: {cashbox.id}, Data: {data}", cashBoxCredentials.CashBoxId, JsonConvert.SerializeObject(data));
                    throw new HelipadException($"Something went wrong on {cashBoxCredentials.QueueId} upload.", x.StatusCode);
                }
            }
        }
        public async Task<long> GetPointerAsync(CashBoxCredentials cashBoxCredentials, Guid queueId, string entityValue)
        {
           using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = _baseAddress;
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxCredentials.CashBoxId.ToString());
                httpClient.DefaultRequestHeaders.Add("accesstoken", cashBoxCredentials.AccessToken);
                httpClient.DefaultRequestHeaders.Add("version", "v0");
                var result = await httpClient.GetAsync($"{_baseAddress}api/service/{entityValue}/pointer/{queueId}");
                var content = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<long>(content);
                }
                else
                {
                    throw new HelipadException($"{content}", result.StatusCode);
                }
            }
        }

        private async Task UploadDataRequestAsync(CashBoxCredentials cashBoxCredentials, string url, dynamic item)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = _baseAddress;
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxCredentials.CashBoxId.ToString());
                httpClient.DefaultRequestHeaders.Add("accesstoken", cashBoxCredentials.AccessToken);

                var requestContent = JsonConvert.SerializeObject(item);

                var response = await httpClient.PutAsync(url, new StringContent(requestContent, Encoding.UTF8, "application/json"));
                if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new HelipadException($"{content}", response.StatusCode);
                }
            }
        }

        public async Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = _baseAddress;
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId.ToString());
                httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
                var result = await httpClient.GetAsync("api/configuration");
                var content = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        throw new HelipadException($"The configuration for {cashBoxId} is empty and therefore not valid.", result.StatusCode);
                    }

                    var configuration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(content);
                    configuration.TimeStamp = DateTime.UtcNow.Ticks;
                    return configuration;
                }
                else
                {
                    throw new HelipadException($"{content}", result.StatusCode);
                }
            }
        }

        private string GetHelipadUrl(string baseAddress) => !baseAddress.EndsWith("/") ? $"{baseAddress}/" : baseAddress;

        public async Task UploadDataAsync(Stream stream, CashBoxCredentials cashBoxCredentials, Guid queueId, string entityValue)
        {
            // HttpClient instead of HttpWebRequest: on .NET 8 HttpWebRequest buffers the whole request body in memory a
            // second time and reports every non-2xx status as a WebException, so a 413 from Helipad never reached the
            // uploader as a HelipadException. StreamContent sends the seekable stream as-is with its Content-Length.
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/service/{entityValue}/{queueId}")
            {
                Content = new StreamContent(stream) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
            };
            request.Headers.Add("cashboxid", cashBoxCredentials.CashBoxId.ToString());
            request.Headers.Add("accesstoken", cashBoxCredentials.AccessToken);
            request.Headers.Add("version", "v0");

            using var response = await _uploadClient.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HelipadException(string.IsNullOrEmpty(responseContent) ? $"{response.ReasonPhrase}" : responseContent, response.StatusCode);
            }
        }       
    }
}
