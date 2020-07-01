using Android.App;
using fiskaltrust.AndroidLauncher.Services.Hosting;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Queue.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace fiskaltrust.AndroidLauncher.Services.Queue
{
    public class QueueProvider
    {
        public IPOS CreatePOS(string workingDir, Dictionary<string, object> queueConfiguration)
        {
            CopyMigrationsToDataDir();

            var queueId = Guid.Parse(queueConfiguration["Id"].ToString());
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queueConfiguration["Configuration"]));
            config["servicefolder"] = workingDir;

            var bootstrapper = new PosBootstrapper
            {
                Configuration = config,
                Id = queueId
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IClientFactory<IDESSCD>, DESSCDClientFactory>();
            serviceCollection.AddLogging();
            bootstrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IPOS>();
        }

        public static void CopyMigrationsToDataDir()
        {
            const string migrationDir = "Migrations";

            var assets = Application.Context.Assets.List(migrationDir);
            var targetDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), migrationDir);
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