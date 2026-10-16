using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.Cart table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class Cart : EntityBase<Guid>
    {
        public Guid CustomerId { get; set; }
        public Guid StoreId { get; set; }
        public Guid AddressId { get; set; }
        public DateTime? PickupWindowStart { get; set; }
        public DateTime? PickupWindowEnd { get; set; }
        public string Status { get; set; } = "OPEN";
        public int Version { get; set; }
    }
}
