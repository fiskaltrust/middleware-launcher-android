using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.AndroidLauncher.Services.SCU
{
    class FiskalyScuProvider : IScuProvider
    {
        public IDESSCD CreateSCU(Dictionary<string, object> scuConfiguration)
        {
            var scuConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(scuConfiguration["Configuration"]));

            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfig,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }
    }
}