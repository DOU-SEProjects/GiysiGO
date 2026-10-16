using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.Order table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class Order : EntityBase<Guid>
    {
        public Guid SourceCartId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid StoreId { get; set; }
        public DateTime PickupWindowStart { get; set; }
        public DateTime PickupWindowEnd { get; set; }
        public string Status { get; set; } = "AWAITING_PICKUP_ASSIGNMENT";
        public decimal EstimatedTotal { get; set; }
        public string Currency { get; set; } = "TRY";
        public string IdempotencyKey { get; set; } = string.Empty;
        public int Version { get; set; }
    }
}
