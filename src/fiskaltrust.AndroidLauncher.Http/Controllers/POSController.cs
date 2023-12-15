using fiskaltrust.ifPOS.v1;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Controllers
{
    public class POSController : Controller
    {
        private readonly IPOS _pos;

        public POSController(IPOS pos)
        {
            _pos = pos;
        }

        [HttpPost("v0/echo")]
        [HttpPost("json/v0/echo")]
        [HttpPost("xml/v0/echo")]
        public ActionResult<string> Echo([FromBody] string request) => _pos.Echo(request);

        [HttpPost("v1/echo")]
        [HttpPost("json/v1/echo")]
        [HttpPost("xml/v1/echo")]
        [HttpPost("echo")]
        public async Task<ActionResult<EchoResponse>> EchoAsync([FromBody] EchoRequest request) => await _pos.EchoAsync(request);

        [HttpPost("v0/sign")]
        public ActionResult<ifPOS.v0.ReceiptResponse> Sign([FromBody] ifPOS.v0.ReceiptRequest request) => _pos.Sign(request);

        [HttpPost("v1/sign")]
        [HttpPost("json/v1/sign")]
        [HttpPost("xml/v1/sign")]
        [HttpPost("sign")]
        public async Task<ActionResult<ReceiptResponse>> SignAsync([FromBody] ReceiptRequest request) => await _pos.SignAsync(request);

        [HttpPost("v0/journal")]
        [HttpPost("json/v0/journal")]
        [HttpPost("xml/v0/journal")]
        [HttpPost("journal")]
        public ActionResult<Stream> Journal([FromQuery] long type, [FromQuery] long from, [FromQuery] long to) => _pos.Journal(type, from, to);
    }
}