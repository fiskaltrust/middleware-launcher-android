using fiskaltrust.ifPOS.v1.it;
using Microsoft.AspNetCore.Mvc;
using System.ServiceModel;
using System.Threading.Tasks;
using static Java.Util.Jar.Attributes;

namespace fiskaltrust.AndroidLauncher.Http.Controllers
{
    public class ITSSCDController : Controller
    {
        private readonly IITSSCD _sscd;

        public ITSSCDController(IITSSCD sscd)
        {
            _sscd = sscd;
        }

        [HttpGet("v1/GetDeviceInfo")]
        public async Task<ActionResult<DeviceInfo>> GetDeviceInfoAsync() => await _sscd.GetDeviceInfoAsync();

        [HttpPost("v1/Echo")]
        public async Task<ActionResult<ScuItEchoResponse>> EchoAsync([FromBody] ScuItEchoRequest request) => await _sscd.EchoAsync(request);

        [HttpPost("v1/FiscalReceiptInvoice")]
        public async Task<ActionResult<FiscalReceiptResponse>> FiscalReceiptInvoiceAsync([FromBody] FiscalReceiptInvoice request) => await _sscd.FiscalReceiptInvoiceAsync(request);

        [HttpPost("v1/FiscalReceiptRefund")]
        public async Task<ActionResult<FiscalReceiptResponse>> FiscalReceiptRefundAsync([FromBody] FiscalReceiptRefund request) => await _sscd.FiscalReceiptRefundAsync(request);

        [OperationContract(Name = "v1/ExecuteDailyClosing")]
        public async Task<ActionResult<DailyClosingResponse>> ExecuteDailyClosingAsync([FromBody] DailyClosingRequest request) => await _sscd.ExecuteDailyClosingAsync(request);

        [HttpPost("v1/NonFiscalReceipt")]
        public async Task<ActionResult<Response>> NonFiscalReceiptAsync([FromBody] NonFiscalRequest request) => await _sscd.NonFiscalReceiptAsync(request);
    }
}