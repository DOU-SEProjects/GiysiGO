using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using GiysiGo.BuildingBlocks.Application.Contracts;
using GiysiGo.BuildingBlocks.Application.Events;
using GiysiGo.BuildingBlocks.Application.Transactions;
using GiysiGo.Modules.Ordering.Application;
using GiysiGo.Modules.Ordering.Application.Contracts;
using GiysiGo.Modules.Ordering.IntegrationEvents;

namespace GiysiGo.Modules.Ordering.Tests
{
    public sealed class OrderingWorkflowServiceTests
    {
        [Fact]
        public async Task Checkout_writes_order_placed_to_outbox_only_for_new_order()
        {
            var repo = new FakeWorkflowRepository
            {
                Checkout = new CheckoutResult(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), true)
            };
            var outbox = new FakeOutboxWriter();
            var transactionRunner = new InlineTransactionRunner();
            var service = CreateService(repo, outbox, runner: transactionRunner);

            var orderId = await service.CheckoutAsync(Guid.NewGuid(), "checkout-key", 2);

            Assert.Equal(repo.Checkout.OrderId, orderId);
            var placed = Assert.IsType<OrderPlacedEvent>(Assert.Single(outbox.Events));
            Assert.Equal(orderId, placed.OrderId);
            Assert.Equal(orderId, placed.EventId);
            Assert.Same(transactionRunner.Connection, outbox.Connection);
            Assert.Same(transactionRunner.Transaction, outbox.Transaction);
        }

        [Fact]
        public async Task Idempotent_checkout_retry_does_not_write_a_second_event()
        {
            var repo = new FakeWorkflowRepository
            {
                ExistingCheckout = new CheckoutResult(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), false)
            };
            var outbox = new FakeOutboxWriter();
            var service = CreateService(repo, outbox);

            var result = await service.CheckoutAsync(Guid.NewGuid(), "checkout-key", 2);

            Assert.Equal(repo.ExistingCheckout!.OrderId, result);
            Assert.Equal(0, repo.CheckoutCallCount);
            Assert.Empty(outbox.Events);
        }

        [Fact]
        public async Task Successful_payment_writes_event_once_and_failure_does_not()
        {
            var repo = new FakeWorkflowRepository
            {
                PaymentCompletion = new PaymentCompletionResult(Guid.NewGuid(), Guid.NewGuid(), true, true)
            };
            var outbox = new FakeOutboxWriter();
            var service = CreateService(repo, outbox);

            await service.CompletePaymentAsync(Guid.NewGuid(), "provider-id", true, DateTime.UtcNow);

            Assert.IsType<PaymentSucceededEvent>(Assert.Single(outbox.Events));
            repo.PaymentCompletion = repo.PaymentCompletion with { Changed = false };
            await service.CompletePaymentAsync(Guid.NewGuid(), "provider-id", true, DateTime.UtcNow);
            Assert.Single(outbox.Events);
            repo.PaymentCompletion = new PaymentCompletionResult(Guid.NewGuid(), Guid.NewGuid(), false, true);
            outbox.Events.Clear();
            await service.CompletePaymentAsync(Guid.NewGuid(), null, false, DateTime.UtcNow);
            Assert.Empty(outbox.Events);
        }

        [Fact]
        public async Task Quote_prepare_requires_store_scoped_permission()
        {
            var permissions = new FakePermissionChecker { Allowed = false };
            var service = CreateService(new FakeWorkflowRepository(), new FakeOutboxWriter(), permissions);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PrepareQuoteLineAsync(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5m, "PIECE", 1m, null));
        }

        [Fact]
        public async Task Rejected_quote_writes_outbox_event_but_acceptance_does_not()
        {
            var repository = new FakeWorkflowRepository();
            var outbox = new FakeOutboxWriter();
            var service = CreateService(repository, outbox);

            await service.DecideQuoteAsync(Guid.NewGuid(), accepted: true);
            Assert.Empty(outbox.Events);
            repository.QuoteDecision = new QuoteDecisionResult(Guid.NewGuid(), Guid.NewGuid(), false, true);
            await service.DecideQuoteAsync(repository.QuoteDecision.QuoteId, accepted: false);
            Assert.IsType<QuoteRejectedEvent>(Assert.Single(outbox.Events));
        }

        private static OrderingWorkflowService CreateService(FakeWorkflowRepository repository, FakeOutboxWriter outbox, FakePermissionChecker? permissions = null, InlineTransactionRunner? runner = null) =>
            new(runner ?? new InlineTransactionRunner(), repository, new FakeAddressReader(), new FakeCatalogReader(), outbox, permissions ?? new FakePermissionChecker(), new FakeCurrentUser());

        private sealed class InlineTransactionRunner : ITransactionRunner
        {
            public StubDbConnection Connection { get; } = new();
            public StubDbTransaction Transaction { get; }
            public InlineTransactionRunner() => Transaction = new StubDbTransaction(Connection);
            public Task<TResult> ExecuteAsync<TResult>(Func<DbConnection, DbTransaction, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default) =>
                action(Connection, Transaction, cancellationToken);
        }

        private sealed class FakeWorkflowRepository : IOrderWorkflowRepository
        {
            public CheckoutResult Checkout { get; set; } = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), true);
            public CheckoutResult? ExistingCheckout { get; set; }
            public int CheckoutCallCount { get; private set; }
            public QuoteDecisionResult QuoteDecision { get; set; } = new(Guid.NewGuid(), Guid.NewGuid(), true, true);
            public PaymentCompletionResult PaymentCompletion { get; set; } = new(Guid.NewGuid(), Guid.NewGuid(), true, true);
            public Task<CheckoutContext?> GetCheckoutContextAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult<CheckoutContext?>(new(cartId, customerId, Guid.NewGuid(), Guid.NewGuid(), 41m, 29m, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2)));
            public Task<Guid?> GetOpenCartStoreIdAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
            public Task<CheckoutResult?> FindCheckoutByIdempotencyAsync(Guid cartId, Guid customerId, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(ExistingCheckout);
            public Task<IReadOnlyList<CheckoutOfferingSelection>> GetCheckoutOfferingSelectionsAsync(Guid cartId, Guid customerId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CheckoutOfferingSelection>>([]);
            public Task<CheckoutResult> CheckoutAsync(Guid cartId, Guid customerId, string idempotencyKey, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) { CheckoutCallCount++; return Task.FromResult(Checkout); }
            public Task SetQuoteLineAsync(Guid quoteId, Guid storeId, Guid orderItemServiceId, decimal unitPrice, string priceUnit, decimal quantity, string? explanation, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SubmitQuoteForApprovalAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task ApproveAndPublishQuoteAsync(Guid quoteId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<OrderTransitionResult> TransitionOrderAsync(Guid orderId, Guid storeId, string expectedStatus, string newStatus, int expectedVersion, Guid? actorUserId, string source, string? note, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(new OrderTransitionResult(orderId, storeId, newStatus, true));
            public Task<Guid> CreateQuoteRevisionAsync(Guid orderId, Guid storeId, Guid actorUserId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());
            public Task<QuoteDecisionResult> RecordCustomerQuoteDecisionAsync(Guid quoteId, Guid customerId, bool accepted, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(QuoteDecision with { QuoteId = quoteId, Accepted = accepted });
            public Task<Guid> BeginPaymentAttemptAsync(Guid orderId, Guid quoteId, Guid customerId, string provider, string idempotencyKey, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());
            public Task<PaymentCompletionResult> CompletePaymentAttemptAsync(Guid paymentAttemptId, string? providerPaymentId, bool succeeded, DateTime completedAtUtc, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(PaymentCompletion);
        }

        private sealed class FakeOutboxWriter : IOutboxWriter
        {
            public List<IIntegrationEvent> Events { get; } = [];
            public Task WriteAsync(IIntegrationEvent integrationEvent, Guid aggregateId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            {
                Events.Add(integrationEvent);
                Connection = connection;
                Transaction = transaction;
                return Task.CompletedTask;
            }
            public DbConnection? Connection { get; private set; }
            public DbTransaction? Transaction { get; private set; }
        }

        private sealed class FakeAddressReader : ICustomerAddressReader
        {
            public Task<CustomerAddressSnapshot?> GetOwnedAddressAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default) =>
                Task.FromResult<CustomerAddressSnapshot?>(new(addressId, customerId, "Address", 41m, 29m, "Name", "Phone"));
        }

        private sealed class FakeCatalogReader : IStoreCatalogReader
        {
            public Task<StoreOfferingSnapshot?> GetOfferingAsync(Guid storeId, Guid productTypeId, Guid serviceId, CancellationToken cancellationToken = default) => Task.FromResult<StoreOfferingSnapshot?>(null);
            public Task<StoreLocationSnapshot?> GetStoreLocationAsync(Guid storeId, CancellationToken cancellationToken = default) => Task.FromResult<StoreLocationSnapshot?>(null);
            public Task<bool> ServesLocationAsync(Guid storeId, decimal latitude, decimal longitude, CancellationToken cancellationToken = default) => Task.FromResult(true);
        }

        private sealed class FakePermissionChecker : IPermissionChecker
        {
            public bool Allowed { get; set; } = true;
            public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? corporateId = null, Guid? storeId = null, CancellationToken cancellationToken = default) => Task.FromResult(Allowed);
        }

        private sealed class FakeCurrentUser : ICurrentUser
        {
            public Guid? UserId { get; } = Guid.NewGuid();
            public bool IsAuthenticated => true;
        }

        private sealed class StubDbConnection : DbConnection
        {
            [AllowNull]
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "test";
            public override string DataSource => "test";
            public override string ServerVersion => "1";
            public override System.Data.ConnectionState State => System.Data.ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => new StubDbTransaction(this);
            protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
        }

        private sealed class StubDbTransaction(DbConnection connection) : DbTransaction
        {
            public override System.Data.IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
            protected override DbConnection DbConnection => connection;
            public override void Commit() { }
            public override void Rollback() { }
        }
    }
}
