using System.Data;
using System.Data.Common;
using Dapper;
using GiysiGo.Modules.Ordering.Application.Contracts;

namespace GiysiGo.Modules.Ordering.Infrastructure.Repositories
{
    public sealed class SqlOrderWorkflowRepository : IOrderWorkflowRepository
    {
        public Task<CheckoutContext?> GetCheckoutContextAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleOrDefaultAsync<CheckoutContext>(new CommandDefinition("[ordering].[spGetCheckoutContext]", new { CartId = cartId, CustomerId = customerId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<Guid?> GetOpenCartStoreIdAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("[ordering].[spGetOpenCartStoreId]", new { CartId = cartId, CustomerId = customerId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<CheckoutResult?> FindCheckoutByIdempotencyAsync(Guid cartId, Guid customerId, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleOrDefaultAsync<CheckoutResult>(new CommandDefinition("[ordering].[spFindCheckoutByIdempotency]", new { CartId = cartId, CustomerId = customerId, IdempotencyKey = idempotencyKey }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task<IReadOnlyList<CheckoutOfferingSelection>> GetCheckoutOfferingSelectionsAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            (await connection.QueryAsync<CheckoutOfferingSelection>(new CommandDefinition("[ordering].[spGetCheckoutOfferingSelections]", new { CartId = cartId, CustomerId = customerId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken))).AsList();

        public Task<CheckoutResult> CheckoutAsync(Guid cartId, Guid customerId, string idempotencyKey, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<CheckoutResult>(new CommandDefinition("[ordering].[spCheckoutCart]", new { CartId = cartId, CustomerId = customerId, IdempotencyKey = idempotencyKey, ExpectedCartVersion = expectedCartVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task SetQuoteLineAsync(Guid quoteId, Guid storeId, Guid orderItemServiceId, decimal unitPrice, string priceUnit, decimal quantity, string? explanation, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spSetDraftQuoteLine]", new { QuoteId = quoteId, StoreId = storeId, OrderItemServiceId = orderItemServiceId, UnitPrice = unitPrice, PriceUnit = priceUnit, Quantity = quantity, Explanation = explanation }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task SubmitQuoteForApprovalAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spSubmitQuoteForApproval]", new { QuoteId = quoteId, StoreId = storeId, ActorUserId = actorUserId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task ApproveAndPublishQuoteAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spApproveAndPublishQuote]", new { QuoteId = quoteId, StoreId = storeId, ActorUserId = actorUserId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<OrderTransitionResult> TransitionOrderAsync(Guid orderId, Guid storeId, string expectedStatus, string newStatus, int expectedVersion, Guid? actorUserId, string source, string? note, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<OrderTransitionResult>(new CommandDefinition("[ordering].[spTransitionOrderStatus]", new { OrderId = orderId, StoreId = storeId, ExpectedStatus = expectedStatus, NewStatus = newStatus, ExpectedVersion = expectedVersion, ActorUserId = actorUserId, Source = source, Note = note }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<Guid> CreateQuoteRevisionAsync(Guid orderId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<Guid>(new CommandDefinition("[ordering].[spCreateQuoteRevision]", new { OrderId = orderId, StoreId = storeId, ActorUserId = actorUserId }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<QuoteDecisionResult> RecordCustomerQuoteDecisionAsync(Guid quoteId, Guid customerId, bool accepted, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<QuoteDecisionResult>(new CommandDefinition("[ordering].[spRecordCustomerQuoteDecision]", new { QuoteId = quoteId, CustomerId = customerId, Accepted = accepted }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<Guid> BeginPaymentAttemptAsync(Guid orderId, Guid quoteId, Guid customerId, string provider, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<Guid>(new CommandDefinition("[ordering].[spBeginPaymentAttempt]", new { OrderId = orderId, QuoteId = quoteId, CustomerId = customerId, Provider = provider, IdempotencyKey = idempotencyKey }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public Task<PaymentCompletionResult> CompletePaymentAttemptAsync(Guid paymentAttemptId, string? providerPaymentId, bool succeeded, DateTime completedAtUtc, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            connection.QuerySingleAsync<PaymentCompletionResult>(new CommandDefinition("[ordering].[spCompletePaymentAttempt]", new { PaymentAttemptId = paymentAttemptId, ProviderPaymentId = providerPaymentId, Succeeded = succeeded, CompletedAtUtc = completedAtUtc }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }
}
