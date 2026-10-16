using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.PriceQuoteLine table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class PriceQuoteLine : EntityBase<Guid>
    {
        public Guid PriceQuoteId { get; set; }
        public Guid OrderItemServiceId { get; set; }
        public string ServiceNameSnapshot { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string PriceUnit { get; set; } = "PIECE";
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public string? Explanation { get; set; }
    }
}
