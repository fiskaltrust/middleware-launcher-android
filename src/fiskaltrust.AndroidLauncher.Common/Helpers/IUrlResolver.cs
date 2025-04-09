using fiskaltrust.storage.serialization.V0;

namespace fiskaltrust.AndroidLauncher.Common.Helpers
{
    public interface IUrlResolver
    {
        string GetProtocolSpecificUrl(PackageConfiguration packageConfiguration);
    }
}