using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Fiskaly;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.AndroidLauncher.Common.Services.SCU
{
    class FiskalyScuProvider : IScuProvider
    {
        public IDESSCD CreateSCU(PackageConfiguration scuConfiguration)
        {
            var bootstrapper = new ScuBootstrapper
            {
                Configuration = scuConfiguration.Configuration,
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogCatLogging();
            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IDESSCD>();
        }
    }
}