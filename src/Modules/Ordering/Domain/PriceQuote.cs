using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.PriceQuote table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class PriceQuote : EntityBase<Guid>
    {
        public Guid OrderId { get; set; }
        public int Revision { get; set; }
        public string Status { get; set; } = "DRAFT";
        public decimal Subtotal { get; set; }
        public decimal PickupFee { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime? ExpiresAt { get; set; }
        public Guid? CustomerDecisionBy { get; set; }
        public DateTime? CustomerDecisionAt { get; set; }
    }
}
