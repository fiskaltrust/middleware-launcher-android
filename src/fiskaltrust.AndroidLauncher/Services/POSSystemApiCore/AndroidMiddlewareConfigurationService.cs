using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.PosSystem.Core.Interfaces;

namespace fiskaltrust.AndroidLauncher.Services.POSSystemApiCore;

internal class AndroidMiddlewareConfigurationService : IMiddlewareConfigurationService
{
    private readonly Guid _cashboxId;
    private readonly string _accessToken;
    private readonly string _configuration;

    public AndroidMiddlewareConfigurationService(Guid cashboxId, string accessToken, string configuration)
    {
        _cashboxId = cashboxId;
        _accessToken = accessToken;
        _configuration = configuration;
    }

    public Task<string?> GetConfiguration(Guid cashboxid, string accesstoken, bool download = false)
    {
        if (cashboxid == _cashboxId
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(accesstoken), Encoding.UTF8.GetBytes(_accessToken)))
        {
            return Task.FromResult<string?>(_configuration);
        }

        return Task.FromResult<string?>(null);
    }
}
