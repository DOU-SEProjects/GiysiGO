using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface IOrderOperationsReader
    {
        Task<OrderOperationsSnapshot?> GetOrderAsync(Guid orderId, Guid storeId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
