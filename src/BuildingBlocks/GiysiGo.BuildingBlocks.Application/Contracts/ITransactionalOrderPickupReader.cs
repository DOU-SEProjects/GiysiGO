using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface ITransactionalOrderPickupReader
    {
        Task<OrderPickupSnapshot?> GetPickupAsync(Guid orderId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
