namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public sealed record OrderServiceSnapshot(
        Guid OrderItemServiceId,
        string ServiceName,
        string PriceUnit,
        decimal EstimatedQuantity,
        decimal? AcceptedQuantity);
}
