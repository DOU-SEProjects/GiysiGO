using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.QuoteApprovalHistory table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class QuoteApprovalHistory : EntityBase<Guid>
    {
        public Guid PriceQuoteId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid ActorUserId { get; set; }
        public string? Reason { get; set; }
    }
}
