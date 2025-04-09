using fiskaltrust.storage.serialization.V0;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public interface IUrlResolver
    {
        string GetProtocolSpecificUrl(PackageConfiguration packageConfiguration);
    }
}