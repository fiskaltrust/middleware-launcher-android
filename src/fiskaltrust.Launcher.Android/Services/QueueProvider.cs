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
        public async Task<IPOS> CreatePosAsync(string workingDir, Dictionary<string, object> queueConfiguration)
        {
            var queueId = Guid.Parse(queueConfiguration["Id"].ToString());
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(queueConfiguration["Configuration"]));
            config["servicefolder"] = workingDir;
                        
            var bootstrapper = new PosBootstrapper();
            return await bootstrapper.CreatePosInstanceAsync(queueId, config);
        }
    }
}