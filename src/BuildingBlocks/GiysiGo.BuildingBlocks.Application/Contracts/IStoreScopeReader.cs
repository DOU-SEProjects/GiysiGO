using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface IStoreScopeReader
    {
        Task<StoreScopeSnapshot?> GetScopeAsync(Guid storeId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
