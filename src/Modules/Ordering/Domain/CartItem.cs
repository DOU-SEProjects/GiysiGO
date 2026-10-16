using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.CartItem table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class CartItem : EntityBase<Guid>
    {
        public Guid CartId { get; set; }
        public Guid ProductTypeId { get; set; }
        public string? CustomerNote { get; set; }
        public decimal? CustomerMeasurement { get; set; }
    }
}
