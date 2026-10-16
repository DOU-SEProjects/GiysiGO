using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.OrderAddressSnapshot table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class OrderAddressSnapshot : EntityBase<Guid>
    {
        public Guid OrderId { get; set; }
        public string AddressText { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
    }
}
