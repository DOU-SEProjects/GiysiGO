using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface IDeliveryTaskReader
    {
        Task<DeliveryTaskSnapshot?> GetTaskAsync(Guid taskId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
