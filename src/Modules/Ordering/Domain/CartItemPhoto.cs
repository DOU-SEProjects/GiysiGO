using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.CartItemPhoto table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class CartItemPhoto : EntityBase<Guid>
    {
        public Guid CartItemId { get; set; }
        public string StorageKey { get; set; } = string.Empty;
        public string PhotoType { get; set; } = string.Empty;
    }
}
