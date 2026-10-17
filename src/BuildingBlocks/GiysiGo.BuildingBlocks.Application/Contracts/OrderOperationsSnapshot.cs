namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public sealed record OrderOperationsSnapshot(
        Guid OrderId,
        Guid StoreId,
        string Status,
        Guid? AcceptedQuoteId,
        bool IsPaid,
        IReadOnlyList<OrderItemSnapshot> Items);
}
