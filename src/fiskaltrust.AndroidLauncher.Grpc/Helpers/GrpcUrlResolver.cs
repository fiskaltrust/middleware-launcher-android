using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.storage.serialization.V0;
using System;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public class GrpcUrlResolver : IUrlResolver
    {
        public string GetProtocolSpecificUrl(PackageConfiguration packageConfiguration)
        {
            var url = packageConfiguration.Url.FirstOrDefault(x => x.StartsWith("grpc"));
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"At least one gRPC URL has to be set in the configuration of the {packageConfiguration.Package} package with the ID {packageConfiguration.Id}.");
            }

            return url;
        }
    }
}