namespace GiysiGo.Modules.Ordering.Application.Contracts
{
    public interface IOrderingWorkflowService
    {
        Task<Guid> CheckoutAsync(Guid cartId, string idempotencyKey, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task DecideQuoteAsync(Guid quoteId, bool accepted, CancellationToken cancellationToken = default);
        Task<Guid> CreateQuoteRevisionAsync(Guid orderId, Guid storeId, CancellationToken cancellationToken = default);
        Task PrepareQuoteLineAsync(Guid quoteId, Guid storeId, Guid orderItemServiceId, decimal unitPrice, string priceUnit, decimal quantity, string? explanation, CancellationToken cancellationToken = default);
        Task SubmitQuoteForApprovalAsync(Guid quoteId, Guid storeId, CancellationToken cancellationToken = default);
        Task ApproveAndPublishQuoteAsync(Guid quoteId, Guid storeId, CancellationToken cancellationToken = default);
        Task<Guid> BeginPaymentAttemptAsync(Guid orderId, Guid quoteId, string provider, string idempotencyKey, CancellationToken cancellationToken = default);
        Task CompletePaymentAsync(Guid paymentAttemptId, string? providerPaymentId, bool succeeded, DateTime completedAtUtc, CancellationToken cancellationToken = default);
        Task TransitionOrderAsync(Guid orderId, Guid storeId, string expectedStatus, string newStatus, int expectedVersion, string source, string? note, CancellationToken cancellationToken = default);
    }
}
