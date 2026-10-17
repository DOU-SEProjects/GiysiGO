using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface ITransactionalStoreCatalogReader
    {
        Task<StoreOfferingSnapshot?> GetOfferingAsync(Guid storeId, Guid productTypeId, Guid serviceId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<StoreLocationSnapshot?> GetStoreLocationAsync(Guid storeId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<bool> ServesLocationAsync(Guid storeId, decimal latitude, decimal longitude, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
