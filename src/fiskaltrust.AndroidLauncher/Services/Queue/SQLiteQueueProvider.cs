using Android.App;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.PosApiPrint;
using fiskaltrust.AndroidLauncher.Services.middleware;
using fiskaltrust.AndroidLauncher.Signing;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Queue.SQLite;
using fiskaltrust.Middleware.SCU.PL.InMemory;
using fiskaltrust.Middleware.Storage.SQLite;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using static fiskaltrust.Middleware.Storage.Base.BaseStorageBootStrapper;

namespace fiskaltrust.AndroidLauncher.Services.Queue
{

    public class SQLiteQueueProvider
    {
        private readonly string sqlLitemigrationsFolder = "Migrations"; 
        public IPOS CreatePOS(string workingDir, PackageConfiguration queueConfiguration, Guid ftCashBoxId, string accessToken, bool isSandbox, LogLevel logLevel, AbstractScuList scus)
        {
            var migrationsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), sqlLitemigrationsFolder);

            CopyMigrationsToDataDir(migrationsPath);

            queueConfiguration.Configuration["servicefolder"] = workingDir;
            queueConfiguration.Configuration["migrationDirectory"] = migrationsPath;

            var bootstrapper = new PosBootstrapper
            {
                Configuration = queueConfiguration.Configuration,
                Id = queueConfiguration.Id
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.TryAddSingleton<IClientFactory<IDESSCD>>(new DESSCDClientFactory(scus.OfType<IDESSCD>()));
            serviceCollection.TryAddSingleton<IClientFactory<IITSSCD>>(new ITSSCDClientFactory(scus.OfType<IITSSCD>()));
            serviceCollection.TryAddSingleton<IClientFactory<IATSSCD>>(new ATSSCDClientFactory(scus.OfType<IATSSCD>(), ftCashBoxId, accessToken));

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
        public POSV2 CreatePOSV2(PackageConfiguration queueConfiguration,string countryCode, ILoggerFactory loggerFactory)
        {
            var workingDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var migrationsPath = Path.Combine(workingDir, sqlLitemigrationsFolder);

            CopyMigrationsToDataDir(migrationsPath);

            queueConfiguration.Configuration["servicefolder"] = workingDir;

            if (countryCode != "PL")
            {
                throw new NotSupportedException($"The market \"{countryCode}\" of the queue {queueConfiguration.Id} is not served by this Android launcher instance. This instance serves only market Poland.");
            }
            var storageProvider = new SQLiteStorageProvider(loggerFactory, queueConfiguration.Id, queueConfiguration.Configuration, new SQLiteStorageConfiguration());
            var queueBootStrapper = countryCode switch
            {

                "PL" => new fiskaltrust.Middleware.Localization.QueuePL.QueuePLBootstrapper(queueConfiguration.Id, loggerFactory, queueConfiguration.Configuration, new InMemorySCU(), storageProvider),
                "AT" or "DE" or "FR" => throw new NotSupportedException($"For markets DE, AT and FR, v1 endpoint should be used."),
                _ => throw new NotSupportedException($"The Countrycode {countryCode} for the Queue with id {queueConfiguration.Id} is not supported."),
            };

            var posV2 = new POSV2((queueBootStrapper.RegisterForEcho(), queueBootStrapper.RegisterForSign(), queueBootStrapper.RegisterForJournal()));

            return posV2;
        }

        private void CopyMigrationsToDataDir(string targetDirectory)
        {
            var assets = Android.App.Application.Context.Assets.List(sqlLitemigrationsFolder);
            Directory.CreateDirectory(targetDirectory);
            foreach (var asset in assets)
            {
                using (var br = new BinaryReader(Android.App.Application.Context.Assets.Open(Path.Combine(sqlLitemigrationsFolder, asset))))
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