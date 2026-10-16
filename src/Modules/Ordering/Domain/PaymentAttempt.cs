using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.PaymentAttempt table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class PaymentAttempt : EntityBase<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid PriceQuoteId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderPaymentId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Status { get; set; } = "INITIATED";
        public DateTime? CompletedAt { get; set; }
    }
}
