using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.POSSystemAPI.Mappings
{
    public class DEMapping : IMiddlewareCaseMapping
    {

        public enum PayItemCaseFlagsNeedMapping : ulong
        {
            Void = 0x0001_0000,
            Refund = 0x0002_0000,
            Reserved = 0x0004_0000,
            Downpayment = 0x0008_0000,
            ForeignCurrency = 0x0010_0000,
            Change = 0x0020_0000,
            Tip = 0x0040_0000,
            Electronic = 0x0080_0000,
            Interface = 0x0100_0000,
            ShowInChargeItems = 0x8000_0000,
        }

        private const ulong DE_COUNTRY_CODE = 0x4445_0000_0000_0000;
        public const ReceiptCase DE_FALLBACK_CODE = (ReceiptCase) 0x4445_2000_FFFF_FFFF;

        private readonly Dictionary<ulong, ulong> _directMappings = new Dictionary<ulong, ulong>
        {
            { 0x4445_2000_0000_0000, 0x4445_0001_0000_0000 },
            { 0x4445_2000_0000_0001, 0x4445_0001_0000_0001 },
            { 0x4445_2000_0000_0002, 0x4445_0001_0000_0011 },
            { 0x4445_2000_0000_0003, 0x4445_0001_0000_0015 },
            { 0x4445_2000_0000_0004, 0x4445_0001_0000_0015 },
            { 0x4445_2000_0000_0005, 0x4445_0001_0000_000F },
            { 0x4445_2000_0000_1000, 0x4445_0001_0000_000E },
            { 0x4445_2000_0000_1001, 0x4445_0001_0000_000D },
            { 0x4445_2000_0000_1002, 0x4445_0001_0000_000C },
            { 0x4445_2000_0000_1003, 0x4445_0001_0000_000E },

            { 0x4445_2000_0000_2000, 0x4445_0001_0000_0002 },
            { 0x4445_2000_0000_2001, 0x4445_0001_0000_0002 },
            { 0x4445_2000_0000_2010, 0x4445_0001_0000_0002 },
            { 0x4445_2000_0000_2011, 0x4445_0001_0000_0007 },
            { 0x4445_2000_0000_2012, 0x4445_0001_0000_0005 },
            { 0x4445_2000_0000_2013, 0x4445_0001_0000_0006 },

            { 0x4445_2000_0000_3001, 0x4445_0001_0000_0014 },
            { 0x4445_2000_0000_3002, 0x4445_0001_0000_0014 },
            { 0x4445_2000_0000_3003, 0x4445_0001_0000_0012 },
            { 0x4445_2000_0000_3004, 0x4445_0001_0000_0010 },
            { 0x4445_2000_0000_3005, 0x4445_0001_0000_0010 },
            { 0x4445_2000_0000_3010, 0x4445_0001_0000_0014 },
            { 0x4445_2000_0000_3011, 0x4445_0001_0000_0014 },

            { 0x4445_2000_0000_4001, 0x4445_0001_0000_0003 },
            { 0x4445_2000_0000_4002, 0x4445_0001_0000_0004 },
            { 0x4445_2000_0000_4011, 0x4445_0001_0000_0010 },
            { 0x4445_2000_0000_4012, 0x4445_0001_0000_0011 },
            { 0x4445_2000_0000_4021, 0x4445_0001_0000_0010 },
            { 0x4445_2000_0000_4022, 0x4445_0001_0000_0011 }
        };


        private readonly Dictionary<ulong, ulong> _directPayItemCaseMappings = new Dictionary<ulong, ulong>
        {
            { 0x4445_2000_0000_0000, 0x4445_0000_0000_0000 },
            { 0x4445_2000_0000_0001, 0x4445_0000_0000_0001 },
            { 0x4445_2000_0000_0002, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0003, 0x4445_0000_0000_0003 },
            { 0x4445_2000_0000_0004, 0x4445_0000_0000_0004 },
            { 0x4445_2000_0000_0005, 0x4445_0000_0000_0005 },
            { 0x4445_2000_0000_0006, 0x4445_0000_0000_000D },
            { 0x4445_2000_0000_0007, 0x4445_0000_0000_0006 },
            { 0x4445_2000_0000_0008, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0009, 0x4445_0000_0000_000E },
            { 0x4445_2000_0000_000A, 0x4445_0000_0000_0008 },
            { 0x4445_2000_0000_000B, 0x4445_0000_0000_0009 },
            // TO be done
            // { 0x4445_2000_0000_000C, ????? },
            { 0x4445_2000_0000_000D, 0x4445_0000_0000_000A },
            { 0x4445_2000_0000_000E, 0x4445_0000_0000_0011 },
            { 0x4445_2000_0000_000F, 0x4445_0000_0000_0006 },
        };

        private readonly Dictionary<ulong, ulong> _directChargeItemCaseMappings = new Dictionary<ulong, ulong>
        {
            { 0x4445_2000_0000_0000, 0x4445_0000_0000_0000 },
            { 0x4445_2000_0000_0001, 0x4445_0000_0000_0002 },
            { 0x4445_2000_0000_0002, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0003, 0x4445_0000_0000_0001 },
            { 0x4445_2000_0000_0004, 0x4445_0000_0000_0003 },
            { 0x4445_2000_0000_0005, 0x4445_0000_0000_0004 },
            { 0x4445_2000_0000_0006, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0007, 0x4445_0000_0000_0006 },
            { 0x4445_2000_0000_0008, 0x4445_0000_0000_0005 },

            { 0x4445_2000_0000_0011, 0x4445_0000_0000_0012 },
            { 0x4445_2000_0000_0012, 0x4445_0000_0000_0017 },
            { 0x4445_2000_0000_0013, 0x4445_0000_0000_0011 },
            { 0x4445_2000_0000_0014, 0x4445_0000_0000_0013 },
            { 0x4445_2000_0000_0015, 0x4445_0000_0000_0014 },
            { 0x4445_2000_0000_0016, 0x4445_0000_0000_0017 },
            { 0x4445_2000_0000_0017, 0x4445_0000_0000_0016 },
            { 0x4445_2000_0000_0018, 0x4445_0000_0000_0015 },

            { 0x4445_2000_0000_0021, 0x4445_0000_0000_001A },
            { 0x4445_2000_0000_0022, 0x4445_0000_0000_001F },
            { 0x4445_2000_0000_0023, 0x4445_0000_0000_0019 },
            { 0x4445_2000_0000_0024, 0x4445_0000_0000_001B },
            { 0x4445_2000_0000_0025, 0x4445_0000_0000_001C },
            { 0x4445_2000_0000_0026, 0x4445_0000_0000_001F },
            { 0x4445_2000_0000_0027, 0x4445_0000_0000_001E },
            { 0x4445_2000_0000_0028, 0x4445_0000_0000_001D },

            { 0x4445_2000_0000_0031, 0x4445_0000_0000_0052 },
            { 0x4445_2000_0000_0032, 0x4445_0000_0000_0057 },
            { 0x4445_2000_0000_0033, 0x4445_0000_0000_0051 },
            { 0x4445_2000_0000_0034, 0x4445_0000_0000_0053 },
            { 0x4445_2000_0000_0035, 0x4445_0000_0000_0054 },
            { 0x4445_2000_0000_0036, 0x4445_0000_0000_0057 },
            { 0x4445_2000_0000_0037, 0x4445_0000_0000_0056 },
            { 0x4445_2000_0000_0038, 0x4445_0000_0000_0059 },

            { 0x4445_2000_0000_0051, 0x4445_0000_0000_0002 },
            { 0x4445_2000_0000_0052, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0053, 0x4445_0000_0000_0001 },
            { 0x4445_2000_0000_0054, 0x4445_0000_0000_0003 },
            { 0x4445_2000_0000_0055, 0x4445_0000_0000_0004 },
            { 0x4445_2000_0000_0056, 0x4445_0000_0000_0007 },
            { 0x4445_2000_0000_0057, 0x4445_0000_0000_0006 },
            { 0x4445_2000_0000_0058, 0x4445_0000_0000_0005 },

            { 0x4445_2000_0000_0061, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0062, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0063, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0064, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0065, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0066, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0067, 0x4445_0000_0000_00A2 },
            { 0x4445_2000_0000_0068, 0x4445_0000_0000_00A2 },

            { 0x4445_2000_0000_0071, 0x4445_0000_0000_0042 },
            { 0x4445_2000_0000_0072, 0x4445_0000_0000_0047 },
            { 0x4445_2000_0000_0073, 0x4445_0000_0000_0041 },
            { 0x4445_2000_0000_0074, 0x4445_0000_0000_0043 },
            { 0x4445_2000_0000_0075, 0x4445_0000_0000_0044 },
            { 0x4445_2000_0000_0076, 0x4445_0000_0000_0047 },
            { 0x4445_2000_0000_0077, 0x4445_0000_0000_0046 },
            { 0x4445_2000_0000_0078, 0x4445_0000_0000_0049 },

            { 0x4445_2000_0000_0081, 0x4445_0000_0000_0042 },
            { 0x4445_2000_0000_0082, 0x4445_0000_0000_0047 },
            { 0x4445_2000_0000_0083, 0x4445_0000_0000_0041 },
            { 0x4445_2000_0000_0084, 0x4445_0000_0000_0043 },
            { 0x4445_2000_0000_0085, 0x4445_0000_0000_0044 },
            { 0x4445_2000_0000_0086, 0x4445_0000_0000_0047 },
            { 0x4445_2000_0000_0087, 0x4445_0000_0000_0046 },
            { 0x4445_2000_0000_0088, 0x4445_0000_0000_0049 }
        };


        private readonly Dictionary<ulong, ulong> _negativeDiscountChargeItemCaseMappings = new Dictionary<ulong, ulong>
        {
            { 0x1, 0x4445_0000_0000_0032 },
            { 0x2, 0x4445_0000_0000_0037 },
            { 0x3, 0x4445_0000_0000_0031 },
            { 0x4, 0x4445_0000_0000_0033 },
            { 0x5, 0x4445_0000_0000_0034 },
            { 0x6, 0x4445_0000_0000_0037 },
            { 0x7, 0x4445_0000_0000_0036 },
            { 0x8, 0x4445_0000_0000_0035 },
        };

        private readonly Dictionary<ulong, ulong> _positiveDiscountChargeItemCaseMappings = new Dictionary<ulong, ulong>
        {
            { 0x1, 0x4445_0000_0000_003A },
            { 0x2, 0x4445_0000_0000_003F },
            { 0x3, 0x4445_0000_0000_0039 },
            { 0x4, 0x4445_0000_0000_003B },
            { 0x5, 0x4445_0000_0000_003C },
            { 0x6, 0x4445_0000_0000_0037 },
            { 0x7, 0x4445_0000_0000_003E },
            { 0x8, 0x4445_0000_0000_003D },
        };

        public ReceiptCase MapV2ToV0ReceiptCase(ReceiptCase v2ReceiptCase, List<ChargeItem> chargeItems, List<PayItem> payItems)
        {
            if (v2ReceiptCase.IsFlag(ifPOS.v2.Cases.ReceiptCaseFlags.Refund))
            {
                v2ReceiptCase = v2ReceiptCase & (ReceiptCase)~ifPOS.v2.Cases.ReceiptCaseFlags.Refund; // Remove the refund flag for mapping
            }            

            // 1. Process direct translations first
            if (_directMappings.TryGetValue((ulong)v2ReceiptCase & 0xFFFF_FFFF_FFFF_FFFF, out ulong directMapping))
            {
                return (ReceiptCase)directMapping;
            }
            return (ReceiptCase) DE_FALLBACK_CODE;
        }

        public PayItemCase MapV2ToV0PayItemCase(PayItemCase v2PayItemCase)
        {
            if (v2PayItemCase.IsFlag(ifPOS.v2.Cases.PayItemCaseFlags.Refund))
            {
                v2PayItemCase = v2PayItemCase & (PayItemCase)~ifPOS.v2.Cases.PayItemCaseFlags.Refund; // Remove the refund flag for mapping
            }

            if (((ulong)v2PayItemCase & 0x0FFF_FFFF_0000) > 0)
            {
                if (((ulong)v2PayItemCase & 0xFFFF_FFFF_0030_0001) == 0x4445_2000_0030_0001)
                {
                    return (PayItemCase)0x4445_0000_0000_000C;
                }

                if (((ulong)v2PayItemCase & 0xFFFF_FFFF_0010_00F1) == 0x4445_2000_0010_0001)
                {
                    return (PayItemCase)0x4445_0000_0000_0002;
                }

                if (((ulong)v2PayItemCase & 0xFFFF_FFFF_0008_00F9) == 0x4445_2000_0008_0009)
                {
                    return (PayItemCase)0x4445_0000_0000_000F;
                }

                if (((ulong)v2PayItemCase & 0xFFFF_FFFF_0040_0001) == 0x4445_2000_0040_0001)
                {
                    return (PayItemCase)0x4445_0000_0040_0012;
                }

                if (((ulong)v2PayItemCase & 0xFFFF_FFFF_0020_0001) == 0x4445_2000_0020_0001)
                {
                    return (PayItemCase)0x4445_0000_0000_000B;
                }
                // flag specific mapping
                throw new Exception($"Failed to map payitemcase '0x{v2PayItemCase:x}'");
            }

            if (_directPayItemCaseMappings.TryGetValue((ulong)v2PayItemCase & 0xFFFF_FFFF_FFFF_FFFF, out ulong directMapping))
            {
                return (PayItemCase)directMapping;
            }
            return (PayItemCase)DE_FALLBACK_CODE;
        }


        public ChargeItemCase MapV2ToV0ChargeItemCase(ChargeItemCase v2ChargeItemCase, decimal chargeItemAmount)
        {
            if (((ulong)v2ChargeItemCase & 0xF_0000) == 0x4_0000) // This is the discount case
            {
                if (chargeItemAmount < 0 && _negativeDiscountChargeItemCaseMappings.TryGetValue((ulong)v2ChargeItemCase & 0xF, out ulong negativeDiscountMapping))
                {
                    return (ChargeItemCase)negativeDiscountMapping;
                }

                if (chargeItemAmount > 0 && _positiveDiscountChargeItemCaseMappings.TryGetValue((ulong)v2ChargeItemCase & 0xF, out ulong positiveDiscountMapping))
                {
                    return (ChargeItemCase)positiveDiscountMapping;
                }
                return (ChargeItemCase)DE_FALLBACK_CODE;
            }

            if (((ulong)v2ChargeItemCase & 0xF0_0000) == 0x20_0000) // This is the take away flag
            {
                var caseForTakeAway = MapV2ToV0ChargeItemCase((ChargeItemCase)((ulong)v2ChargeItemCase & 0x4445_2000_0000_FFFF), chargeItemAmount);
                return (ChargeItemCase)((ulong)caseForTakeAway | 0x1_0000);
            }

            if (((ulong)v2ChargeItemCase & 0x1_0000) == 0x0001_0000) // This is the void flag
            {
                var caseForTakeAway = MapV2ToV0ChargeItemCase((ChargeItemCase)((ulong)v2ChargeItemCase & 0x4445_2000_0000_FFFF), chargeItemAmount);
                return (ChargeItemCase)((ulong)caseForTakeAway | 0x2_0000);
            }

            if (((ulong)v2ChargeItemCase & 0x2_0000) == 0x0002_0000) // This is the void flag
            {
                var caseForTakeAway = MapV2ToV0ChargeItemCase((ChargeItemCase)((ulong)v2ChargeItemCase & 0x4445_2000_0000_FFFF), chargeItemAmount);
                return (ChargeItemCase)((ulong)caseForTakeAway | 0x2_0000);
            }

            if (_directChargeItemCaseMappings.TryGetValue((ulong)v2ChargeItemCase, out ulong directMapping))
            {
                return (ChargeItemCase)directMapping;
            }
            return (ChargeItemCase)DE_FALLBACK_CODE;
        }

        public State MapV0ToV2ftState(State ftState)
        {
            return (ulong)ftState switch
            {
                0x4445_0000_0000_0000 => (State)0x4445_2000_0000_0000,
                0x4445_0000_0000_0001 => (State)0x4445_2000_0000_0001,
                0x4445_0000_0000_0002 => (State)0x4445_2000_0000_0002,
                0x4445_0000_0000_0008 => (State)0x4445_2000_0000_0008,
                0x4445_0000_0000_0040 => (State)0x4445_2000_0000_0040,
                _ => (State)((ulong)ftState | 0x4445_2000_0000_0000),
            };
        }

        public SignatureType MapV0ToV2ftSignatureType(SignatureType ftSignatureType)
        {
            return (SignatureType)((ulong)ftSignatureType | 0x4445_2000_0000_0000);
        }
    }
}
