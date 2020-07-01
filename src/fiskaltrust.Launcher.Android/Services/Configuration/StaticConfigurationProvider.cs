using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Services.Configuration
{
    class StaticConfigurationProvider : IConfigurationProvider
    {
        private const string CONFIG = @"{
  ""helpers"": [],
  ""ftCashBoxId"": ""82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14"",
  ""ftSignaturCreationDevices"": [
    {
      ""Id"": ""471c6b4f-03dc-4dd5-a216-d76e8776ec85"",
      ""Package"": ""fiskaltrust.Middleware.SCU.DE.Fiskaly"",
      ""Version"": ""1.3.0-rc1-gb4e76d551b"",
      ""Configuration"": {
        ""TssId"": ""8f23353c-67b3-4af9-8b54-3370034328b7"",
        ""ApiKey"": ""test_1iwrxfk97b9qx6ufn9q9yw8ce_ftfiskalytest"",
        ""ApiSecret"": ""UOPJZwvNfZRxQyMaEQwglJ5TmJBNZDe0VBfZZrQTePq"",
        ""test"": ""test""
      },
      ""Url"": [
        ""grpc://localhost:10301""
      ]
    }
  ],
  ""ftQueues"": [
    {
      ""Id"": ""b80af2a1-b7f8-4aa4-938f-9043b3a5ae40"",
      ""Package"": ""fiskaltrust.Middleware.Queue.SQLite"",
      ""Version"": ""0.0.0.0"",
      ""Configuration"": {
        ""init_ftCashBox"": {
          ""ftCashBoxId"": ""82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14"",
          ""TimeStamp"": 637173856684469990
        },
        ""init_ftQueue"": [
          {
            ""ftQueueId"": ""b80af2a1-b7f8-4aa4-938f-9043b3a5ae40"",
            ""ftCashBoxId"": ""82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14"",
            ""ftCurrentRow"": 0,
            ""ftQueuedRow"": 0,
            ""ftReceiptNumerator"": 0,
            ""ftReceiptTotalizer"": 0.0,
            ""ftReceiptHash"": null,
            ""StartMoment"": null,
            ""StopMoment"": null,
            ""CountryCode"": ""DE"",
            ""Timeout"": 15000,
            ""TimeStamp"": 637173723196714614
          }
        ],
        ""init_ftQueueDE"": [
          {
            ""ftQueueDEId"": ""b80af2a1-b7f8-4aa4-938f-9043b3a5ae40"",
            ""ftSignaturCreationUnitDEId"": ""471c6b4f-03dc-4dd5-a216-d76e8776ec85"",
            ""LastHash"": null,
            ""CashBoxIdentification"": ""E1HKfn68Pkms5zsZsvKONw"",
            ""TimeStamp"": 637173618228639776
          }
        ],
        ""init_ftSignaturCreationUnitAT"": [],
        ""init_ftSignaturCreationUnitDE"": [
          {
            ""ftSignaturCreationUnitDEId"": ""471c6b4f-03dc-4dd5-a216-d76e8776ec85"",
            ""Url"": ""[\""grpc://localhost:10301\""]"",
            ""TimeStamp"": 637173856608128962
          }
        ],
        ""init_ftSignaturCreationUnitFR"": []
      },
      ""Url"": [
        ""grpc://localhost:10300""
      ]
    }
  ],
  ""TimeStamp"": 637173893392226560
}";

        public async Task<Dictionary<string, object>> GetCashboxConfigurationAsync(Guid cashboxId)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(CONFIG);
        }
    }
}