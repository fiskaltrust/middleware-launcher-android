using fiskaltrust.ifPOS.v1;
using System;

namespace fiskaltrust.AndroidLauncher.PosApiPrint.Models
{
    public class PrintRequest
    {
        public ReceiptRequest request { get; set; }
        public ReceiptResponse response { get; set; }
    }
}