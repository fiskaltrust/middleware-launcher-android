using fiskaltrust.AndroidLauncher.Helpers;
using fiskaltrust.storage.serialization.V0;
using System;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Http.Helpers
{
    public class RestUrlResolver : IUrlResolver
    {
        public string GetProtocolSpecificUrl(PackageConfiguration packageConfiguration)
        {
            if (packageConfiguration.Package.StartsWith("fiskaltrust.Middleware.Queue"))
            {
                var url = packageConfiguration.Url.FirstOrDefault(x => x.StartsWith("rest"));
                if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentException($"At least one REST URL has to be set in the configuration of the {packageConfiguration.Package} package with the ID {packageConfiguration.Id}.");
                }

                return url.Replace("rest://", "http://");
            }
            else
            {
                return packageConfiguration.Url.FirstOrDefault();
            }
        }
    }
}