using System;
using Grpc.Core;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Grpc.Configuration;
using System.Reflection;

namespace fiskaltrust.AndroidLauncher.Helpers.Hosting
{
    public static class GrpcHelper
    {
        public static Server StartHost<T>(string url, T service) where T : class
        {
            var baseAddresse = new Uri(url);
            var server = new Server();
            server.Ports.Add(new ServerPort(baseAddresse.Host, baseAddresse.Port, ServerCredentials.Insecure));

            // We use versioned names in our OperationContracts, e.g. v1/Sign. This works fine in C#, but not with regular .proto files, 
            // as they don't support special characters. To work around this issue, we register the methods twice with different 
            // behavior - once with the v1/ prefix, and once without it.
            server.Services.AddCodeFirst(service, BinderConfiguration.Create(binder: new RemoveMethodVersionPrefixBinder()));
            server.Services.AddCodeFirst(service, BinderConfiguration.Create(binder: new SkipNonVersionedMethodsBinder()));

            server.Start();
            return server;
        }
    }

    internal class RemoveMethodVersionPrefixBinder : ServiceBinder
    {
        public override bool IsOperationContract(MethodInfo method, out string name)
        {
            var result = base.IsOperationContract(method, out name);
            if (name.Contains("/"))
            {
                name = name.Substring(name.IndexOf("/") + 1);
            }

            return result;
        }
    }

    internal class SkipNonVersionedMethodsBinder : ServiceBinder
    {
        public override bool IsOperationContract(MethodInfo method, out string name)
        {
            base.IsOperationContract(method, out name);
            return name.Contains("/");
        }
    }
}