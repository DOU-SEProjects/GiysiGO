using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.OrderItemPhoto table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class OrderItemPhoto : EntityBase<Guid>
    {
        public Guid OrderItemId { get; set; }
        public string StorageKey { get; set; } = string.Empty;
        public string PhotoType { get; set; } = string.Empty;
    }
}
