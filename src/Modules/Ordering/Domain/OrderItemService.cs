using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.OrderItemService table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class OrderItemService : EntityBase<Guid>
    {
        public Guid OrderItemId { get; set; }
        public Guid ProductTypeServiceId { get; set; }
        public string ServiceNameSnapshot { get; set; } = string.Empty;
        public decimal EstimatedUnitPrice { get; set; }
        public string PriceUnitSnapshot { get; set; } = "PIECE";
        public decimal EstimatedQuantity { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string Status { get; set; } = "REQUESTED";
    }
}
