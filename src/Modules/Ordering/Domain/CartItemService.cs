using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.CartItemService table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class CartItemService : EntityBase<Guid>
    {
        public Guid CartItemId { get; set; }
        public Guid StoreOfferingId { get; set; }
    }
}
