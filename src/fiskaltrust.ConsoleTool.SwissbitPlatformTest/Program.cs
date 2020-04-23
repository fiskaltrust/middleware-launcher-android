using fiskaltrust.Middleware.SCU.DE.Swissbit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace fiskaltrust.ConsoleTool.SwissbitPlatformTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine($"usage: {Assembly.GetExecutingAssembly().GetName().Name} swissbit-mount-point [swissbit-library-file]");
                return;
            }

            var configuration = new Dictionary<string, object>()
                {
                    { "devicePath", args[0] },
                };

            if (args.Length >= 2)
            {
                configuration.Add("libraryFile", args[1]);
            }

            using (var scu = new SwissbitSCU(configuration))
            {
                Console.WriteLine($"instanciate swissbit on {args[0]}");

                await scu.WaitForInitialization();
                Console.WriteLine($"initialization done");

                var tseInfo = await scu.GetTseInfoAsync();
                Console.WriteLine(JsonConvert.SerializeObject(tseInfo));
            }

        }
    }
}
