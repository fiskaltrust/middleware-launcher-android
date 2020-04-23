using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Queue.SQLite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Launcher.Android
{
    public class QueueProvider
    {
        public async Task<IPOS> CreatePos(string workingDir, Dictionary<string, object> configuration)
        {
            var queues = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(configuration["ftQueues"]));
            var queue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queues[0]));
            var queueConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queue["Configuration"]));
            queueConfiguration["servicefolder"] = workingDir;

            var queueId = Guid.Parse(queue["Id"].ToString());
            
            var bootstrapper = new PosBootstrapper();
            return await bootstrapper.CreatePosInstanceAsync(queueId, queueConfiguration);
        }
    }
}