using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using Newtonsoft.Json;
using System.Text.Json;

namespace fiskaltrust.Api.POS.Signing;

public static class ReceiptRequestHelper
{
    public static ifPOS.v1.ReceiptRequest ConvertToV1(ReceiptRequest data)
    {
        var receiptRequest = new ifPOS.v1.ReceiptRequest
        {
            cbReceiptAmount = data.cbReceiptAmount,
            cbReceiptMoment = data.cbReceiptMoment,
            cbReceiptReference = data.cbReceiptReference,
            cbTerminalID = data.cbTerminalID ?? "",
            ftCashBoxID = data.ftCashBoxID?.ToString(),
            ftPosSystemId = data.ftPosSystemId?.ToString(),
            ftQueueID = data.ftQueueID?.ToString(),
            ftReceiptCase = (long)data.ftReceiptCase,
            cbChargeItems = data.cbChargeItems?.Select(x => ConvertToV1(x, data.DecimalPrecisionMultiplier)).ToArray() ?? Array.Empty<ifPOS.v1.ChargeItem>(),
            cbPayItems = data.cbPayItems?.Select(x => ConvertToV1(x, data.DecimalPrecisionMultiplier)).ToArray() ?? Array.Empty<ifPOS.v1.PayItem>()
        };


        if (data.cbPreviousReceiptReference != null)
        {
            receiptRequest.cbPreviousReceiptReference = data.cbPreviousReceiptReference?.Match(
                single => single,
                group => JsonConvert.SerializeObject(group)
            );
        }


        if (data.cbSettlement is System.Text.Json.JsonElement cbSettlementElem)
        {
            if (cbSettlementElem.ValueKind == JsonValueKind.String)
            {
                receiptRequest.cbSettlement = cbSettlementElem.Deserialize<string>();
            }
            else if (cbSettlementElem.ValueKind == JsonValueKind.Object)
            {
                receiptRequest.cbSettlement = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(cbSettlementElem))));
            }
        }

        if (data.cbUser is System.Text.Json.JsonElement cbUserElem)
        {
            if (cbUserElem.ValueKind == JsonValueKind.String)
            {
                receiptRequest.cbUser = cbUserElem.Deserialize<string>();
            }
            else if (cbUserElem.ValueKind == JsonValueKind.Object)
            {
                receiptRequest.cbUser = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(cbUserElem))));
            }
        }

        if (data.cbArea is System.Text.Json.JsonElement cbAreaElem)
        {
            if (cbAreaElem.ValueKind == JsonValueKind.String)
            {
                receiptRequest.cbArea = cbAreaElem.Deserialize<string>();
            }
            else if (cbAreaElem.ValueKind == JsonValueKind.Object)
            {
                receiptRequest.cbArea = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(cbAreaElem))));
            }
        }

        if (data.cbCustomer is System.Text.Json.JsonElement cbCustomerElem)
        {
            if (cbCustomerElem.ValueKind == JsonValueKind.String)
            {
                receiptRequest.cbCustomer = cbCustomerElem.Deserialize<string>();
            }
            else if (cbCustomerElem.ValueKind == JsonValueKind.Object)
            {
                receiptRequest.cbCustomer = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(cbCustomerElem))));
            }
        }

        if (data.ftReceiptCaseData is System.Text.Json.JsonElement ftReceiptCaseDataElem)
        {
            if (ftReceiptCaseDataElem.ValueKind == JsonValueKind.String)
            {
                receiptRequest.ftReceiptCaseData = ftReceiptCaseDataElem.Deserialize<string>();
            }
            else if (ftReceiptCaseDataElem.ValueKind == JsonValueKind.Object)
            {
                receiptRequest.ftReceiptCaseData = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(ftReceiptCaseDataElem))));
            }
        }
        return receiptRequest;
    }

    public static ifPOS.v1.ReceiptResponse ConvertToV1(ReceiptResponse data)
    {
        var response = new ifPOS.v1.ReceiptResponse
        {
            ftCashBoxID = data.ftCashBoxID.ToString(),
            cbReceiptReference = data.cbReceiptReference,
            cbTerminalID = data.cbTerminalID,
            ftCashBoxIdentification = data.ftCashBoxIdentification,
            ftChargeLines = data.ftChargeLines?.ToArray(),
            ftPayLines = data.ftPayLines?.ToArray(),
            ftQueueID = data.ftQueueID.ToString(),
            ftQueueItemID = data.ftQueueItemID.ToString(),
            ftQueueRow = data.ftQueueRow,
            ftReceiptFooter = data.ftReceiptFooter?.ToArray(),
            ftReceiptHeader = data.ftReceiptHeader?.ToArray(),
            ftReceiptIdentification = data.ftReceiptIdentification,
            ftReceiptMoment = data.ftReceiptMoment,
            ftState = (long)data.ftState,
            ftChargeItems = data.ftChargeItems?.Select(ConvertToV1).ToArray(),
            ftPayItems = data.ftPayItems?.Select(ConvertToV1).ToArray(),
            ftSignatures = data.ftSignatures?.Select(ConvertToV1).ToArray(),
        };
        if (data.ftStateData is System.Text.Json.JsonElement ftStateDataElem)
        {
            if (ftStateDataElem.ValueKind == JsonValueKind.String)
            {
                response.ftStateData = ftStateDataElem.Deserialize<string>();
            }
            else if (ftStateDataElem.ValueKind == JsonValueKind.Object)
            {
                response.ftStateData = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(ftStateDataElem))));
            }
        }
        return response;
    }

    public static ifPOS.v1.SignaturItem ConvertToV1(SignatureItem data)
    {
        return new ifPOS.v1.SignaturItem
        {
            Caption = data.Caption,
            Data = data.Data,
            ftSignatureFormat = (long)data.ftSignatureFormat,
            ftSignatureType = (long)data.ftSignatureType
        };
    }

    public static ifPOS.v1.ChargeItem ConvertToV1(ChargeItem data, int decimalPrecision)
    {
        var chargeItem = new ifPOS.v1.ChargeItem
        {
            AccountNumber = data.AccountNumber,
            Amount = data.Amount == 0m ? 0 : data.Amount / decimalPrecision,
            CostCenter = data.CostCenter,
            Description = data.Description,
            ftChargeItemCase = (long)data.ftChargeItemCase,
            Moment = data.Moment,
            Position = data.Position == 0m ? 0 : Convert.ToInt64(data.Position / decimalPrecision),
            ProductBarcode = data.ProductBarcode,
            ProductGroup = data.ProductGroup,
            ProductNumber = data.ProductNumber,
            Quantity = data.Quantity == 0m ? 0 : data.Quantity / decimalPrecision,
            Unit = data.Unit,
            UnitPrice = data.UnitPrice.HasValue ? data.UnitPrice / decimalPrecision : null,
            UnitQuantity = data.UnitQuantity.HasValue ? data.UnitQuantity / decimalPrecision : null,
            VATAmount = data.VATAmount.HasValue ? data.VATAmount / decimalPrecision : null,
            VATRate = data.VATRate == 0m ? 0 : data.VATRate / decimalPrecision
        };
        if (data.ftChargeItemCaseData is System.Text.Json.JsonElement ftChargeItemCaseDataElem)
        {
            if (ftChargeItemCaseDataElem.ValueKind == JsonValueKind.String)
            {
                chargeItem.ftChargeItemCaseData = ftChargeItemCaseDataElem.Deserialize<string>();
            }
            else if (ftChargeItemCaseDataElem.ValueKind == JsonValueKind.Object)
            {
                chargeItem.ftChargeItemCaseData = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(ftChargeItemCaseDataElem))));
            }
        }
        return chargeItem;
    }

    public static ifPOS.v1.PayItem ConvertToV1(PayItem data, int decimalPrecision)
    {
        var payItem = new ifPOS.v1.PayItem
        {
            AccountNumber = data.AccountNumber,
            Amount = data.Amount == 0 ? 0 : data.Amount / decimalPrecision,
            CostCenter = data.CostCenter,
            Description = data.Description,
            ftPayItemCase = (long)data.ftPayItemCase,
            Moment = data.Moment,
            Position = data.Position == 0 ? 0 : Convert.ToInt64(data.Position / decimalPrecision),
            MoneyGroup = data.MoneyGroup,
            MoneyNumber = data.MoneyNumber,
            Quantity = data.Quantity == 0 ? 0 : data.Quantity / decimalPrecision
        };
        if (data.ftPayItemCaseData is System.Text.Json.JsonElement ftPayItemCaseDataElem)
        {
            if (ftPayItemCaseDataElem.ValueKind == JsonValueKind.String)
            {
                payItem.ftPayItemCaseData = ftPayItemCaseDataElem.Deserialize<string>();
            }
            else if (ftPayItemCaseDataElem.ValueKind == JsonValueKind.Object)
            {
                payItem.ftPayItemCaseData = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Dictionary<string, object>>((System.Text.Json.JsonSerializer.Serialize(ftPayItemCaseDataElem))));
            }
        }
        return payItem;
    }

    public static ReceiptResponse ConvertToV2(ifPOS.v1.ReceiptResponse data)
    {
        return new ReceiptResponse
        {
            ftCashBoxID = Guid.Parse(data.ftCashBoxID),
            cbReceiptReference = data.cbReceiptReference,
            cbTerminalID = data.cbTerminalID,
            ftCashBoxIdentification = data.ftCashBoxIdentification,
            ftChargeLines = data.ftChargeLines?.ToList(),
            ftPayLines = data.ftPayLines?.ToList(),
            ftQueueID = Guid.Parse(data.ftQueueID),
            ftQueueItemID = Guid.Parse(data.ftQueueItemID),
            ftQueueRow = data.ftQueueRow,
            ftReceiptFooter = data.ftReceiptFooter?.ToList(),
            ftReceiptHeader = data.ftReceiptHeader?.ToList(),
            ftReceiptIdentification = data.ftReceiptIdentification,
            ftReceiptMoment = data.ftReceiptMoment,
            ftState = (State)data.ftState,
            ftStateData = data.ftStateData,
            ftChargeItems = data.ftChargeItems?.Select(ConvertToV2).ToList(),
            ftPayItems = data.ftPayItems?.Select(ConvertToV2).ToList(),
            ftSignatures = data.ftSignatures?.Select(ConvertToV2).ToList(),
        };
    }

    public static SignatureItem ConvertToV2(ifPOS.v1.SignaturItem data)
    {
        return new SignatureItem
        {
            Caption = data.Caption,
            Data = data.Data,
            ftSignatureFormat = (SignatureFormat)data.ftSignatureFormat,
            ftSignatureType = (SignatureType)data.ftSignatureType
        };
    }

    public static ChargeItem ConvertToV2(ifPOS.v1.ChargeItem data)
    {
        return new ChargeItem
        {
            AccountNumber = data.AccountNumber,
            Amount = data.Amount,
            CostCenter = data.CostCenter,
            Description = data.Description,
            ftChargeItemCase = (ChargeItemCase)data.ftChargeItemCase,
            ftChargeItemCaseData = data.ftChargeItemCaseData,
            Moment = data.Moment,
            Position = data.Position,
            ProductBarcode = data.ProductBarcode,
            ProductGroup = data.ProductGroup,
            ProductNumber = data.ProductNumber,
            Quantity = data.Quantity,
            Unit = data.Unit,
            UnitPrice = data.UnitPrice,
            UnitQuantity = data.UnitQuantity,
            VATAmount = data.VATAmount,
            VATRate = data.VATRate
        };
    }

    public static PayItem ConvertToV2(ifPOS.v1.PayItem data)
    {
        return new PayItem
        {
            AccountNumber = data.AccountNumber,
            Amount = data.Amount,
            CostCenter = data.CostCenter,
            Description = data.Description,
            ftPayItemCase = (PayItemCase)data.ftPayItemCase,
            ftPayItemCaseData = data.ftPayItemCaseData,
            Moment = data.Moment,
            Position = data.Position,
            MoneyGroup = data.MoneyGroup,
            MoneyNumber = data.MoneyNumber,
            Quantity = data.Quantity
        };
    }
}
