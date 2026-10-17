using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface IOrderInspectionWriter
    {
        Task RecordInspectionAsync(Guid orderId, Guid storeId, Guid orderItemId, bool accepted, decimal? verifiedMeasurement, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
