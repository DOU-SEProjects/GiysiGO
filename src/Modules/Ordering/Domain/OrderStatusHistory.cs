using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.OrderStatusHistory table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class OrderStatusHistory : EntityBase<Guid>
    {
        public Guid OrderId { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public Guid? ActorUserId { get; set; }
        public string Source { get; set; } = "SYSTEM";
        public string? Note { get; set; }
    }
}
