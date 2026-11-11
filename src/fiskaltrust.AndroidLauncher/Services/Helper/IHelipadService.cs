using fiskaltrust.AndroidLauncher.Models;
using fiskaltrust.storage.serialization.V0;

namespace fiskaltrust.AndroidLauncher.Services.Helper
{
    public interface IHelipadService
    {
        Task UploadeQueuePLListAsync(CashBoxCredentials cashBoxCredentials, List<dynamic> data);
        Task UploadQueueListAsync(CashBoxCredentials cashBoxCredentials, List<dynamic> data);
        Task UploadSignatureCreationUnitPLListAsync(CashBoxCredentials cashBoxCredentials, List<dynamic> data);


        Task<long> GetPointerAsync(CashBoxCredentials cashBoxCredentials, Guid queueId, string entityValue);

        Task UploadDataAsync(Stream stream, CashBoxCredentials cashBoxCredentials, Guid queueId, string entityValue);

        Task<ftCashBoxConfiguration> GetConfigurationAsync(Guid cashBoxId, string accessToken);
    }
}
