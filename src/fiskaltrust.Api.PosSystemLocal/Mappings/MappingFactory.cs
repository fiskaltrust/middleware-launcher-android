using fiskaltrust.ifPOS.v2;
using fiskaltrust.Api.POS.Signing;
using fiskaltrust.POSSystemAPI.Mappings;
using fiskaltrust.ifPOS.v2.Cases;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1.it;

public class MappingFactory
{
    public static void AddCountryCodeIfNeeded(ReceiptRequest request, string queueCountryCode)
    {
        if (request.ftReceiptCase.CountryCode() == 0 && request.ftReceiptCase.Version() == 2)
        {
            var countryCode = queueCountryCode?.ToUpper();
            request.ftReceiptCase = request.ftReceiptCase.WithCountry(countryCode);
        }

        foreach (var chargeItem in request.cbChargeItems)
        {
            if (chargeItem.ftChargeItemCase.CountryCode() == 0 && chargeItem.ftChargeItemCase.Version() == 2)
            {
                var countryCode = queueCountryCode?.ToUpper();
                chargeItem.ftChargeItemCase = chargeItem.ftChargeItemCase.WithCountry(countryCode);
            }
        }

        foreach (var payItem in request.cbPayItems)
        {
            if (payItem.ftPayItemCase.CountryCode() == 0 && payItem.ftPayItemCase.Version() == 2)
            {
                var countryCode = queueCountryCode.ToUpper();
                payItem.ftPayItemCase = payItem.ftPayItemCase.WithCountry(countryCode);
            }
        }
    }

    public static fiskaltrust.ifPOS.v1.ReceiptRequest ConvertV2ToV1Request(ReceiptRequest request, string queueCountryCode)
    {
        if (((ulong)request.ftReceiptCase & (ulong)0x2000_0000_0000) != 0x2000_0000_0000)
        {
            return ReceiptRequestHelper.ConvertToV1(request);
        }

        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            var mappings = new ATMapping();

            request.ftReceiptCaseData = new
            {
                v2ReceiptRequest = request,
            };

            request.ftReceiptCase = mappings.MapV2ToV0ReceiptCase(request.ftReceiptCase, request.cbChargeItems, request.cbPayItems);
            foreach (var chargeItem in request.cbChargeItems)
            {
                chargeItem.ftChargeItemCase = mappings.MapV2ToV0ChargeItemCase(chargeItem.ftChargeItemCase, chargeItem.Amount);
            }
            foreach (var payItem in request.cbPayItems)
            {
                payItem.ftPayItemCase = mappings.MapV2ToV0PayItemCase(payItem.ftPayItemCase);
            }
            return ReceiptRequestHelper.ConvertToV1(request);
        }
        else if (countryCode == "DE")
        {
            var mappings = new DEMapping();

            request.ftReceiptCaseData = new
            {
                v2ReceiptRequest = request,
            };

            request.ftReceiptCase = mappings.MapV2ToV0ReceiptCase(request.ftReceiptCase, request.cbChargeItems, request.cbPayItems);
            foreach (var chargeItem in request.cbChargeItems)
            {
                chargeItem.ftChargeItemCase = mappings.MapV2ToV0ChargeItemCase(chargeItem.ftChargeItemCase, chargeItem.Amount);
            }
            foreach (var payItem in request.cbPayItems)
            {
                payItem.ftPayItemCase = mappings.MapV2ToV0PayItemCase(payItem.ftPayItemCase);
            }
            return ReceiptRequestHelper.ConvertToV1(request);
        }
        else if (countryCode == "FR")
        {
            var mappings = new FRMapping();

            request.ftReceiptCaseData = new
            {
                v2ReceiptRequest = request,
            };

            request.ftReceiptCase = mappings.MapV2ToV0ReceiptCase(request.ftReceiptCase, request.cbChargeItems, request.cbPayItems);
            foreach (var chargeItem in request.cbChargeItems)
            {
                chargeItem.ftChargeItemCase = mappings.MapV2ToV0ChargeItemCase(chargeItem.ftChargeItemCase, chargeItem.Amount);
            }
            foreach (var payItem in request.cbPayItems)
            {
                payItem.ftPayItemCase = mappings.MapV2ToV0PayItemCase(payItem.ftPayItemCase);
            }
            return ReceiptRequestHelper.ConvertToV1(request);
        }
        throw new Exception($"Mapping to V1 for {countryCode} is not supported.");
    }

    public static fiskaltrust.ifPOS.v1.ReceiptRequest ConvertV2CodeToV1Request(fiskaltrust.ifPOS.v1.ReceiptRequest request, string queueCountryCode)
    {
        if (((ulong)request.ftReceiptCase & (ulong)0x2000_0000_0000) != 0x2000_0000_0000)
        {
            return request;
        }
        IMiddlewareCaseMapping mapping;
        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            mapping = new ATMapping();
        }
        else if (countryCode == "DE")
        {
            mapping = new DEMapping();
        }
        else if (countryCode == "FR")
        {

            mapping = new FRMapping();
        }
        else
        {
            return request;
        }

        request.ftReceiptCaseData = JsonConvert.SerializeObject(new
        {
            v2ReceiptRequest = request,
        });

        request.ftReceiptCase = (long)mapping.MapV2ToV0ReceiptCase((ReceiptCase)request.ftReceiptCase, [], []);
        foreach (var chargeItem in request.cbChargeItems)
        {
            chargeItem.ftChargeItemCase = (long)mapping.MapV2ToV0ChargeItemCase((ChargeItemCase)chargeItem.ftChargeItemCase, chargeItem.Amount);
        }
        foreach (var payItem in request.cbPayItems)
        {
            payItem.ftPayItemCase = (long)mapping.MapV2ToV0PayItemCase((PayItemCase)payItem.ftPayItemCase);
        }
        return request;
    }

    public static fiskaltrust.ifPOS.v1.ReceiptResponse ConvertV2CodeToV1Response(fiskaltrust.ifPOS.v1.ReceiptResponse response, string queueCountryCode)
    {
        if (((ulong)response.ftState & (ulong)0x2000_0000_0000) != 0x2000_0000_0000)
        {
            return response;
        }

        IMiddlewareCaseMapping mapping;
        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            mapping = new ATMapping();
        }
        else if (countryCode == "DE")
        {
            mapping = new DEMapping();
        }
        else if (countryCode == "FR")
        {

            mapping = new FRMapping();
        }
        else
        {
            return response;
        }

        var data = response;
        data.ftState = (long)mapping.MapV0ToV2ftState((State)response.ftState);
        foreach (var signatureItem in data.ftSignatures)
        {
            signatureItem.ftSignatureType = (long)mapping.MapV0ToV2ftSignatureType((SignatureType)signatureItem.ftSignatureType);
        }
        return data;
    }

    public static fiskaltrust.ifPOS.v1.ReceiptRequest ConvertV2CodeToV1Request(fiskaltrust.ifPOS.v2.ReceiptRequest request, string queueCountryCode)
    {
        if (((ulong)request.ftReceiptCase & (ulong)0x2000_0000_0000) != 0x2000_0000_0000)
        {
            return ReceiptRequestHelper.ConvertToV1(request);
        }

        IMiddlewareCaseMapping mapping;
        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            mapping = new ATMapping();
        }
        else if (countryCode == "DE")
        {
            mapping = new DEMapping();
        }
        else if (countryCode == "FR")
        {

            mapping = new FRMapping();
        }
        else
        {
            return ReceiptRequestHelper.ConvertToV1(request);
        }
        request.ftReceiptCaseData = JsonConvert.SerializeObject(new
        {
            v2ReceiptRequest = request,
        });
        request.ftReceiptCase = mapping.MapV2ToV0ReceiptCase((ReceiptCase)request.ftReceiptCase, request.cbChargeItems, request.cbPayItems);
        foreach (var chargeItem in request.cbChargeItems)
        {
            chargeItem.ftChargeItemCase = mapping.MapV2ToV0ChargeItemCase((ChargeItemCase)chargeItem.ftChargeItemCase, chargeItem.Amount);
        }
        foreach (var payItem in request.cbPayItems)
        {
            payItem.ftPayItemCase = mapping.MapV2ToV0PayItemCase((PayItemCase)payItem.ftPayItemCase);
        }
        return ReceiptRequestHelper.ConvertToV1(request);
    }

    public static fiskaltrust.ifPOS.v1.ReceiptResponse ConvertV2CodeToV1Response(fiskaltrust.ifPOS.v2.ReceiptResponse response, string queueCountryCode)
    {
        if (((ulong)response.ftState & (ulong)0x2000_0000_0000) != 0x2000_0000_0000)
        {
            return ReceiptRequestHelper.ConvertToV1(response);
        }


        IMiddlewareCaseMapping mapping;
        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            mapping = new ATMapping();
        }
        else if (countryCode == "DE")
        {
            mapping = new DEMapping();
        }
        else if (countryCode == "FR")
        {

            mapping = new FRMapping();
        }
        else
        {
            return ReceiptRequestHelper.ConvertToV1(response);
        }

        var data = response;
        data.ftState = mapping.MapV0ToV2ftState((State)response.ftState);
        foreach (var signatureItem in data.ftSignatures)
        {
            signatureItem.ftSignatureType = mapping.MapV0ToV2ftSignatureType((SignatureType)signatureItem.ftSignatureType);
        }
        return ReceiptRequestHelper.ConvertToV1(data);
    }

    public static ReceiptResponse ConvertV1ToV2Response(fiskaltrust.ifPOS.v1.ReceiptResponse response, string queueCountryCode)
    {
        if ((response.ftState & 0x2000_0000_0000) == 0x2000_0000_0000)
        {
            return ReceiptRequestHelper.ConvertToV2(response);
        }

        var countryCode = queueCountryCode?.ToUpper();
        if (countryCode == "AT")
        {
            var data = ReceiptRequestHelper.ConvertToV2(response);
            var mappings = new ATMapping();
            data.ftState = mappings.MapV0ToV2ftState((State)response.ftState);
            foreach (var signatureItem in data.ftSignatures)
            {
                signatureItem.ftSignatureType = mappings.MapV0ToV2ftSignatureType(signatureItem.ftSignatureType);
            }
            return data;
        }
        else if (countryCode == "DE")
        {
            var data = ReceiptRequestHelper.ConvertToV2(response);
            var mappings = new DEMapping();
            data.ftState = mappings.MapV0ToV2ftState((State)response.ftState);
            foreach (var signatureItem in data.ftSignatures)
            {
                signatureItem.ftSignatureType = mappings.MapV0ToV2ftSignatureType(signatureItem.ftSignatureType);
            }
            return data;
        }
        else if (countryCode == "FR")
        {
            var data = ReceiptRequestHelper.ConvertToV2(response);
            var mappings = new FRMapping();
            data.ftState = mappings.MapV0ToV2ftState((State)response.ftState);
            foreach (var signatureItem in data.ftSignatures)
            {
                signatureItem.ftSignatureType = mappings.MapV0ToV2ftSignatureType(signatureItem.ftSignatureType);
            }
            return data;
        }
        throw new Exception($"Mapping to V1 for {countryCode} is not supported.");
    }
}
