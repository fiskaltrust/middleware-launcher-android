using System;
using Grpc.Core;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Grpc.Client;

namespace fiskaltrust.Launcher.Android
{
    public static class GrpcHelper
    {
        public static Server StartHost<T>(string url, T service) where T : class
        {
            var baseAddresse = new Uri(url);
            var server = new Server();
            server.Ports.Add(new ServerPort(baseAddresse.Host, baseAddresse.Port, ServerCredentials.Insecure));
            server.Services.AddCodeFirst(service);
            server.Start();
            return server;
        }

        public static T GetClient<T>(string url) where T : class
        {
            var channel = new Channel(url, ChannelCredentials.Insecure);
            channel.ConnectAsync(DateTime.UtcNow.AddSeconds(30)).Wait();

            return channel.CreateGrpcService<T>();
        }
    }
}