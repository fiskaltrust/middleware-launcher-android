using Android.App;
using fiskaltrust.AndroidLauncher.Extensions;
using fiskaltrust.AndroidLauncher.Helpers.Hosting;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.SQLite;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace fiskaltrust.AndroidLauncher.Services.Queue
{
    public class SQLiteQueueProvider
    {
        public IPOS CreatePOS(string workingDir, PackageConfiguration queueConfiguration)
        {
            var migrationsFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Migrations");

            CopyMigrationsToDataDir(migrationsFolder);
            
            queueConfiguration.Configuration["servicefolder"] = workingDir;
            queueConfiguration.Configuration["migrationDirectory"] = migrationsFolder;

            var bootstrapper = new PosBootstrapper
            {
                Configuration = queueConfiguration.Configuration,
                Id = queueConfiguration.Id
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IClientFactory<IDESSCD>, DESSCDClientFactory>();
            serviceCollection.AddLogCatLogging();

            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IPOS>();
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