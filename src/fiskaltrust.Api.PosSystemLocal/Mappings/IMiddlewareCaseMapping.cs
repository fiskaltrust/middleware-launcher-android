using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.POSSystemAPI.Mappings
{
    public interface IMiddlewareCaseMapping
    {
        SignatureType MapV0ToV2ftSignatureType(SignatureType ftSignatureType);
        State MapV0ToV2ftState(State ftState);
        ChargeItemCase MapV2ToV0ChargeItemCase(ChargeItemCase v2ChargeItemCase, decimal chargeItemAmount);
        PayItemCase MapV2ToV0PayItemCase(PayItemCase v2PayItemCase);
        ReceiptCase MapV2ToV0ReceiptCase(ReceiptCase v2ReceiptCase, List<ChargeItem> chargeItems, List<PayItem> payItems);
    }
}