using fiskaltrust.ifPOS.v1.de;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Grpc.Controllers
{
    public class DESSCDController : Controller
    {
        private readonly IDESSCD _sscd;

        public DESSCDController(IDESSCD sscd)
        {
            _sscd = sscd;
        }

        [HttpPost("v1/starttransaction")]
        public async Task<ActionResult<StartTransactionResponse>> StartTransactionAsync([FromBody] StartTransactionRequest request) => await _sscd.StartTransactionAsync(request);

        [HttpPost("v1/updatetransaction")]
        public async Task<ActionResult<UpdateTransactionResponse>> UpdateTransactionAsync([FromBody] UpdateTransactionRequest request) => await _sscd.UpdateTransactionAsync(request);

        [HttpPost("v1/finishtransaction")]
        public async Task<ActionResult<FinishTransactionResponse>> FinishTransactionAsync([FromBody] FinishTransactionRequest request) => await _sscd.FinishTransactionAsync(request);

        [HttpGet("v1/tseinfo")]
        public async Task<ActionResult<TseInfo>> GetTseInfoAsync() => await _sscd.GetTseInfoAsync();

        [HttpPost("v1/tsestate")]
        public async Task<ActionResult<TseState>> SetTseStateAsync([FromBody] TseState request) => await _sscd.SetTseStateAsync(request);

        [HttpPost("v1/registerclientid")]
        public async Task<ActionResult<RegisterClientIdResponse>> RegisterClientIdAsync([FromBody] RegisterClientIdRequest request) => await _sscd.RegisterClientIdAsync(request);

        [HttpPost("v1/unregisterclientid")]
        public async Task<ActionResult<UnregisterClientIdResponse>> UnregisterClientIdAsync([FromBody] UnregisterClientIdRequest request) => await _sscd.UnregisterClientIdAsync(request);

        [HttpPost("v1/executesettsetime")]
        public async Task ExecuteSetTseTimeAsync() => await _sscd.ExecuteSetTseTimeAsync();

        [HttpPost("v1/executeselftest")]
        public async Task ExecuteSelfTestAsync() => await _sscd.ExecuteSelfTestAsync();

        [HttpPost("v1/startexportsession")]
        public async Task<ActionResult<StartExportSessionResponse>> StartExportSessionAsync([FromBody] StartExportSessionRequest request) => await _sscd.StartExportSessionAsync(request);

        [HttpPost("v1/startexportsessionbytimestamp")]
        public async Task<ActionResult<StartExportSessionResponse>> StartExportSessionByTimeStampAsync([FromBody] StartExportSessionByTimeStampRequest request) => await _sscd.StartExportSessionByTimeStampAsync(request);

        [HttpPost("v1/startexportsessionbytransaction")]
        public async Task<ActionResult<StartExportSessionResponse>> StartExportSessionByTransactionAsync([FromBody] StartExportSessionByTransactionRequest request) => await _sscd.StartExportSessionByTransactionAsync(request);

        [HttpPost("v1/exportdata")]
        public async Task<ActionResult<ExportDataResponse>> ExportDataAsync([FromBody] ExportDataRequest request) => await _sscd.ExportDataAsync(request);

        [HttpPost("v1/endexportsession")]
        public async Task<ActionResult<EndExportSessionResponse>> EndExportSessionAsync([FromBody] EndExportSessionRequest request) => await _sscd.EndExportSessionAsync(request);

        [HttpPost("v1/echo")]
        public async Task<ActionResult<ScuDeEchoResponse>> EchoAsync([FromBody] ScuDeEchoRequest request) => await _sscd.EchoAsync(request);
    }
}