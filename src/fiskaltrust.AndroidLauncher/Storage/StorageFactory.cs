using fiskaltrust.Api.PosSystem.Core.Interfaces;
namespace fiskaltrust.AndroidLauncher.Storage;

public class StorageFactory : IStorageFactory
{
  private readonly IOperationItemRepository _operationItemRepository;
  public StorageFactory(IOperationItemRepository operationItemRepository)
  {
    _operationItemRepository = operationItemRepository;
  }
  public async Task<IOperationItemRepository> CreateAsync(Guid queueId)
  {
    return _operationItemRepository;
  }


}


