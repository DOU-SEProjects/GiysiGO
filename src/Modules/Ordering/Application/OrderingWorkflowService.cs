using GiysiGo.BuildingBlocks.Application.Contracts;
using GiysiGo.BuildingBlocks.Application.Events;
using GiysiGo.BuildingBlocks.Application.Transactions;
using GiysiGo.Modules.Ordering.Application.Contracts;
using GiysiGo.Modules.Ordering.IntegrationEvents;

namespace GiysiGo.Modules.Ordering.Application
{
    public sealed class OrderingWorkflowService(
        ITransactionRunner transactionRunner,
        IOrderWorkflowRepository repository,
        ICustomerAddressReader addressReader,
        IStoreCatalogReader catalogReader,
        IOutboxWriter outboxWriter,
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser) : IOrderingWorkflowService
    {
        public async Task<Guid> CheckoutAsync(Guid cartId, string idempotencyKey, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
                throw new ArgumentException("Idempotency key must contain 1 to 200 characters.", nameof(idempotencyKey));

            return await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                var previousCheckout = await repository.FindCheckoutByIdempotencyAsync(cartId, customerId, idempotencyKey, connection, transaction, token);
                if (previousCheckout is not null) return previousCheckout.OrderId;
                var context = await repository.GetCheckoutContextAsync(cartId, customerId, connection, transaction, token)
                    ?? throw new InvalidOperationException("Open cart was not found for the customer.");
                var address = await addressReader.GetOwnedAddressAsync(customerId, context.AddressId, token)
                    ?? throw new UnauthorizedAccessException("Cart address is not owned by the customer.");
                if (address.Latitude != context.Latitude || address.Longitude != context.Longitude)
                    throw new InvalidOperationException("Cart address changed during checkout; reload the cart.");
                if (!await catalogReader.ServesLocationAsync(context.StoreId, address.Latitude, address.Longitude, token))
                    throw new InvalidOperationException("Store does not serve the selected address.");
                var selectedOfferings = await repository.GetCheckoutOfferingSelectionsAsync(cartId, customerId, connection, transaction, token);
                foreach (var selection in selectedOfferings)
                {
                    var offering = await catalogReader.GetOfferingAsync(context.StoreId, selection.ProductTypeId, selection.ServiceId, token);
                    if (offering is null || !offering.IsActive || offering.StoreId != context.StoreId || offering.ProductTypeId != selection.ProductTypeId || offering.ServiceId != selection.ServiceId)
                        throw new InvalidOperationException("A selected service is no longer available for this store and product.");
                }

                var result = await repository.CheckoutAsync(cartId, customerId, idempotencyKey, expectedCartVersion, connection, transaction, token);
                if (result.Created)
                {
                    var integrationEvent = new OrderPlacedEvent(result.OrderId, DateTime.UtcNow, result.OrderId, result.StoreId,
                        DateTime.SpecifyKind(result.PickupWindowStartUtc, DateTimeKind.Utc), DateTime.SpecifyKind(result.PickupWindowEndUtc, DateTimeKind.Utc));
                    await outboxWriter.WriteAsync(integrationEvent, result.OrderId, connection, transaction, token);
                }
                return result.OrderId;
            }, cancellationToken);
        }

        public async Task<Guid> CreateQuoteRevisionAsync(Guid orderId, Guid storeId, CancellationToken cancellationToken = default)
        {
            var actorUserId = RequireCurrentUserId();
            if (!await permissionChecker.HasPermissionAsync(actorUserId, PermissionCodes.QuotePrepare, storeId: storeId, cancellationToken: cancellationToken))
                throw new UnauthorizedAccessException("Missing quote.prepare permission for this store.");
            return await transactionRunner.ExecuteAsync((connection, transaction, token) =>
                repository.CreateQuoteRevisionAsync(orderId, storeId, actorUserId, connection, transaction, token), cancellationToken);
        }

        public async Task DecideQuoteAsync(Guid quoteId, bool accepted, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                var decision = await repository.RecordCustomerQuoteDecisionAsync(quoteId, customerId, accepted, connection, transaction, token);
                if (decision.Changed && !decision.Accepted)
                {
                    var integrationEvent = new QuoteRejectedEvent(quoteId, DateTime.UtcNow, decision.OrderId, quoteId);
                    await outboxWriter.WriteAsync(integrationEvent, decision.OrderId, connection, transaction, token);
                }
                return true;
            }, cancellationToken);
        }

        public async Task PrepareQuoteLineAsync(Guid quoteId, Guid storeId, Guid orderItemServiceId, decimal unitPrice, string priceUnit, decimal quantity, string? explanation, CancellationToken cancellationToken = default)
        {
            var actorUserId = RequireCurrentUserId();
            if (!await permissionChecker.HasPermissionAsync(actorUserId, PermissionCodes.QuotePrepare, storeId: storeId, cancellationToken: cancellationToken))
                throw new UnauthorizedAccessException("Missing quote.prepare permission for this store.");
            if (unitPrice < 0 || quantity <= 0 || (priceUnit != "PIECE" && priceUnit != "M2"))
                throw new ArgumentOutOfRangeException(nameof(unitPrice), "Quote line values are outside the allowed range.");
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await repository.SetQuoteLineAsync(quoteId, storeId, orderItemServiceId, unitPrice, priceUnit, quantity, explanation, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task SubmitQuoteForApprovalAsync(Guid quoteId, Guid storeId, CancellationToken cancellationToken = default)
        {
            var actorUserId = RequireCurrentUserId();
            if (!await permissionChecker.HasPermissionAsync(actorUserId, PermissionCodes.QuotePrepare, storeId: storeId, cancellationToken: cancellationToken))
                throw new UnauthorizedAccessException("Missing quote.prepare permission for this store.");
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await repository.SubmitQuoteForApprovalAsync(quoteId, storeId, actorUserId, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task ApproveAndPublishQuoteAsync(Guid quoteId, Guid storeId, CancellationToken cancellationToken = default)
        {
            var actorUserId = RequireCurrentUserId();
            if (!await permissionChecker.HasPermissionAsync(actorUserId, PermissionCodes.QuoteApprove, storeId: storeId, cancellationToken: cancellationToken))
                throw new UnauthorizedAccessException("Missing quote.approve permission for this store.");
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await repository.ApproveAndPublishQuoteAsync(quoteId, storeId, actorUserId, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task<Guid> BeginPaymentAttemptAsync(Guid orderId, Guid quoteId, string provider, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            if (string.IsNullOrWhiteSpace(provider) || provider.Length > 60)
                throw new ArgumentException("Provider must contain 1 to 60 characters.", nameof(provider));
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
                throw new ArgumentException("Idempotency key must contain 1 to 200 characters.", nameof(idempotencyKey));
            return await transactionRunner.ExecuteAsync((connection, transaction, token) =>
                repository.BeginPaymentAttemptAsync(orderId, quoteId, customerId, provider, idempotencyKey, connection, transaction, token), cancellationToken);
        }

        public async Task CompletePaymentAsync(Guid paymentAttemptId, string? providerPaymentId, bool succeeded, DateTime completedAtUtc, CancellationToken cancellationToken = default)
        {
            if (completedAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Payment completion time must be UTC.", nameof(completedAtUtc));
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                var result = await repository.CompletePaymentAttemptAsync(paymentAttemptId, providerPaymentId, succeeded, completedAtUtc, connection, transaction, token);
                if (result.Changed && result.Succeeded)
                {
                    var integrationEvent = new PaymentSucceededEvent(paymentAttemptId, completedAtUtc, result.OrderId, result.QuoteId);
                    await outboxWriter.WriteAsync(integrationEvent, result.OrderId, connection, transaction, token);
                }
                return true;
            }, cancellationToken);
        }

        public async Task TransitionOrderAsync(Guid orderId, Guid storeId, string expectedStatus, string newStatus, int expectedVersion, string source, string? note, CancellationToken cancellationToken = default)
        {
            Guid? actorUserId = source == "SYSTEM" ? null : RequireCurrentUserId();
            if (source == "SYSTEM")
            {
                if (actorUserId is not null) throw new ArgumentException("System transitions cannot specify a user actor.", nameof(actorUserId));
            }
            else
            {
                if (source is not ("STORE" or "MOCK_COURIER") || actorUserId is null)
                    throw new UnauthorizedAccessException("This transition source is not allowed for a user command.");
                if (!await permissionChecker.HasPermissionAsync(actorUserId.Value, PermissionCodes.OrderFulfill, storeId: storeId, cancellationToken: cancellationToken))
                    throw new UnauthorizedAccessException("Missing order.fulfill permission for this store.");
            }
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                var result = await repository.TransitionOrderAsync(orderId, storeId, expectedStatus, newStatus, expectedVersion, actorUserId, source, note, connection, transaction, token);
                if (result.Changed && result.Status == "READY_FOR_DELIVERY")
                {
                    var integrationEvent = new OrderReadyForDeliveryEvent(Guid.NewGuid(), DateTime.UtcNow, orderId);
                    await outboxWriter.WriteAsync(integrationEvent, orderId, connection, transaction, token);
                }
                return true;
            }, cancellationToken);
        }

        private Guid RequireCurrentUserId() => currentUser.IsAuthenticated && currentUser.UserId is Guid userId
            ? userId
            : throw new UnauthorizedAccessException("An authenticated user is required for this operation.");
    }
}
