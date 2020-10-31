using fiskaltrust.ifPOS.v1;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Grpc.Controllers
{
    public class POSController : Controller
    {
        private readonly IPOS _pos;

        public POSController(IPOS pos)
        {
            _pos = pos;
        }

        [HttpPost("api/v0/echo")]
        public ActionResult<string> Echo([FromBody] string request) => _pos.Echo(request);

        [HttpPost("api/v1/echo")]
        public async Task<ActionResult<EchoResponse>> EchoAsync([FromBody] EchoRequest request) => await _pos.EchoAsync(request);

        [HttpPost("api/v0/sign")]
        public ActionResult<ifPOS.v0.ReceiptResponse> Sign([FromBody] ifPOS.v0.ReceiptRequest request) => _pos.Sign(request);

        [HttpPost("api/v1/sign")]
        public async Task<ActionResult<ReceiptResponse>> SignAsync([FromBody] ReceiptRequest request) => await _pos.SignAsync(request);

        [HttpPost("api/v0/journal")]
        public ActionResult<Stream> Journal([FromQuery] long type, [FromQuery] long from, [FromQuery] long to) => _pos.Journal(type, from, to);
    }
}