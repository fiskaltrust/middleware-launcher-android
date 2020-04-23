using fiskaltrust.Middleware.Storage.SQLite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Launcher.Android.Storage
{
    public class SqliteStorageProvider
    {
        public async Task InitializeAsync(string workingDir, Dictionary<string, object> configuration)
        {
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var azureQueue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(azureQueue["Configuration"]));
            queueConfiguration.Add("servicefolder", workingDir);
            
            var sqliteBootstrapper = new SQLiteStorageBootstrapper();
            await sqliteBootstrapper.InitAsync(Guid.Parse(azureQueue["Id"].ToString()), queueConfiguration);
        }
    }
}