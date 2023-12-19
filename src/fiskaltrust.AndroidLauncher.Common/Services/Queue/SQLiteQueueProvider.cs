using Android.App;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Queue.SQLite;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection.Extensions;
using fiskaltrust.AndroidLauncher.Common.Signing;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.AndroidLauncher.Common.PosApiPrint;

namespace fiskaltrust.AndroidLauncher.Common.Services.Queue
{
 
    public class SQLiteQueueProvider
    {
        public IPOS CreatePOS(string workingDir, PackageConfiguration queueConfiguration, Guid ftCashBoxId, string accessToken, bool isSandbox, LogLevel logLevel, AbstractScuList scus)
        {
            var migrationsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Migrations");

            CopyMigrationsToDataDir(migrationsFolder);

            queueConfiguration.Configuration["servicefolder"] = workingDir;
            queueConfiguration.Configuration["migrationDirectory"] = migrationsFolder;

            var bootstrapper = new PosBootstrapper
            {
                Configuration = queueConfiguration.Configuration,
                Id = queueConfiguration.Id
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.TryAddSingleton<IClientFactory<IDESSCD>>(new DESSCDClientFactory(scus.OfType<IDESSCD>()));
            serviceCollection.TryAddSingleton<IClientFactory<IITSSCD>>(new ITSSCDClientFactory(scus.OfType<IITSSCD>()));

            serviceCollection.AddLogProviders(logLevel);
            serviceCollection.AddAppInsights(Helpers.Configuration.GetAppInsightsInstrumentationKey(isSandbox), "fiskaltrust.Middleware.Queue.SQLite", ftCashBoxId);

            bootstrapper.ConfigureServices(serviceCollection);
            var services = serviceCollection.BuildServiceProvider();
            var pos = services.GetRequiredService<IPOS>();
            if (queueConfiguration.Configuration.ContainsKey("useposapi"))
            {
                var posApiHelper = new PosApiHelper(new PosApiProvider(ftCashBoxId, accessToken, isSandbox ? new Uri("https://pos-api-sandbox.fiskaltrust.cloud/") : new Uri("https://pos-api.fiskaltrust.cloud/"), services.GetRequiredService<ILogger<PosApiProvider>>()), pos, services.GetRequiredService<ILogger<PosApiHelper>>());
                return posApiHelper;
            }
            return pos;
        }

        public static void CopyMigrationsToDataDir(string targetDirectory)
        {
            const string migrationDir = "Migrations";

            var assets = Application.Context.Assets.List(migrationDir);
            Directory.CreateDirectory(targetDirectory);
            foreach (var asset in assets)
            {
                using (var br = new BinaryReader(Application.Context.Assets.Open(Path.Combine(migrationDir, asset))))
                {
                    var targetFile = Path.Combine(targetDirectory, asset);
                    if (!File.Exists(targetFile))
                    {
                        using (var bw = new BinaryWriter(new FileStream(targetFile, FileMode.Create)))
                        {
                            byte[] buffer = new byte[2048];
                            int length = 0;
                            while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                bw.Write(buffer, 0, length);
                            }
                        }
                    }
                }
            }
        }
    }
}