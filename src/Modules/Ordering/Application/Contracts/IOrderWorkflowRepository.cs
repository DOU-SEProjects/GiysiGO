using System.Data.Common;

namespace GiysiGo.Modules.Ordering.Application.Contracts
{
    /// <summary>Business transitions for orders, quotes and payments; deliberately has no generic update/delete.</summary>
    public interface IOrderWorkflowRepository
    {
        Task<CheckoutContext?> GetCheckoutContextAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid?> GetOpenCartStoreIdAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<CheckoutResult?> FindCheckoutByIdempotencyAsync(Guid cartId, Guid customerId, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CheckoutOfferingSelection>> GetCheckoutOfferingSelectionsAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<CheckoutResult> CheckoutAsync(Guid cartId, Guid customerId, string idempotencyKey, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task SetQuoteLineAsync(Guid quoteId, Guid storeId, Guid orderItemServiceId, decimal unitPrice, string priceUnit, decimal quantity, string? explanation, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task SubmitQuoteForApprovalAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task ApproveAndPublishQuoteAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<OrderTransitionResult> TransitionOrderAsync(Guid orderId, Guid storeId, string expectedStatus, string newStatus, int expectedVersion, Guid? actorUserId, string source, string? note, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid> CreateQuoteRevisionAsync(Guid orderId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<QuoteDecisionResult> RecordCustomerQuoteDecisionAsync(Guid quoteId, Guid customerId, bool accepted, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid> BeginPaymentAttemptAsync(Guid orderId, Guid quoteId, Guid customerId, string provider, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<PaymentCompletionResult> CompletePaymentAttemptAsync(Guid paymentAttemptId, string? providerPaymentId, bool succeeded, DateTime completedAtUtc, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }

    public sealed record CheckoutResult(Guid OrderId, Guid SourceCartId, Guid StoreId, DateTime PickupWindowStartUtc, DateTime PickupWindowEndUtc, bool Created);
    public sealed record CheckoutContext(Guid CartId, Guid CustomerId, Guid StoreId, Guid AddressId, decimal Latitude, decimal Longitude, DateTime PickupWindowStartUtc, DateTime PickupWindowEndUtc);
    public sealed record CheckoutOfferingSelection(Guid ProductTypeId, Guid ServiceId);
    public sealed record QuoteDecisionResult(Guid OrderId, Guid QuoteId, bool Accepted, bool Changed);
    public sealed record PaymentCompletionResult(Guid OrderId, Guid QuoteId, bool Succeeded, bool Changed);
    public sealed record OrderTransitionResult(Guid OrderId, Guid StoreId, string Status, bool Changed);
}
