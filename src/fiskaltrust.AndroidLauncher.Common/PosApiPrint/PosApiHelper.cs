using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.AndroidLauncher.Common.PosApiPrint.Helpers;

namespace fiskaltrust.AndroidLauncher.Common.PosApiPrint
{
    public class PosApiHelper : IPOS
    {
        private readonly PosApiProvider _posApiProvider;
        private IPOS _targetPOS;

        public PosApiHelper(PosApiProvider provider, IPOS targetPos)
        {
            _posApiProvider = provider;
            _targetPOS = targetPos;
        }

        public async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
        {
            var response = await _targetPOS.SignAsync(request);
            await _posApiProvider.PrintAsync(request, response);
            return response;
        }

        public IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request) => _targetPOS.JournalAsync(request);

        public Task<EchoResponse> EchoAsync(EchoRequest message) => _targetPOS.EchoAsync(message);

        public ifPOS.v0.ReceiptResponse Sign(ifPOS.v0.ReceiptRequest data) => Task.Run(() => SignAsync(data)).Result;

        private async Task<ifPOS.v0.ReceiptResponse> SignAsync(ifPOS.v0.ReceiptRequest data) => ReceiptRequestHelper.ConvertToV0(await SignAsync(ReceiptRequestHelper.ConvertToV1(data)));

        private delegate ifPOS.v0.ReceiptResponse Sign_Delegate(ifPOS.v0.ReceiptRequest request);
        public IAsyncResult BeginSign(ifPOS.v0.ReceiptRequest data, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(data, callback, d);
            return r;
        }

        public ifPOS.v0.ReceiptResponse EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }

        public Stream Journal(long ftJournalType, long from, long to) => _targetPOS.Journal(ftJournalType, from, to);

        private delegate Stream Journal_Delegate(long ftJournalType, long from, long to);
        public IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state)
        {
            var d = new Journal_Delegate(Journal);
            var r = d.BeginInvoke(ftJournalType, from, to, callback, d);
            return r;
        }

        public Stream EndJournal(IAsyncResult result)
        {
            var d = (Journal_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }

        public string Echo(string message) => _targetPOS.Echo(message);

        private delegate string Echo_Delegate(string message);
        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }
    }
}
