namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public sealed record OrderItemSnapshot(
        Guid OrderItemId,
        Guid ProductTypeId,
        string ProductTypeName,
        string TrackingCode,
        string? CustomerNote,
        decimal? CustomerMeasurement,
        decimal? VerifiedMeasurement,
        IReadOnlyList<OrderPhotoSnapshot> Photos,
        IReadOnlyList<OrderServiceSnapshot> Services);
}
