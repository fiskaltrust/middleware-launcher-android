using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.AndroidLauncher.PosApiPrint.Models;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.PosApiPrint
{
    public class PosApiProvider
    {
        private readonly ILogger<PosApiProvider> _logger;
        private readonly HttpClient _httpClient;

        public PosApiProvider(Guid cashBoxId, string accessToken, Uri posApiurl, ILogger<PosApiProvider> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient(new HttpClientHandler { }, disposeHandler: true) { BaseAddress = posApiurl };
            _httpClient.DefaultRequestHeaders.Add("cashboxid", cashBoxId.ToString());
            _httpClient.DefaultRequestHeaders.Add("accesstoken", accessToken);
            _logger.LogDebug("Communciation with POS API ({PosApiUrl}) for CashBox {CashBoxId}.", posApiurl, cashBoxId);
        }

        public async Task PrintAsync(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
        {
            _logger.LogDebug("Try to print receipt with queueitemid {ftqueueitemid}", receiptResponse.ftQueueItemID);
            try
            {
                var printResponse = await _httpClient.PostAsync("v0/print", new StringContent(JsonConvert.SerializeObject(new PrintRequest
                {
                    request = receiptRequest,
                    response = receiptResponse
                }), Encoding.UTF8, "application/json"));

                var result = await printResponse.Content.ReadAsStringAsync();
                if (!printResponse.IsSuccessStatusCode)
                {
                    throw new Exception(result);
                }
                _logger.LogDebug("Submitted Receipt for printing with queueitemid {ftqueueitemid}", receiptResponse.ftQueueItemID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print receipt with queueitemid {ftqueueitemid}. Reason: {reason}", receiptResponse.ftQueueItemID, ex.Message);
            }
        }
    }
}
