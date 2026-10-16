using GiysiGo.BuildingBlocks.Application.Contracts;
using GiysiGo.BuildingBlocks.Application.Transactions;
using GiysiGo.Modules.Ordering.Application.Contracts;
using GiysiGo.Modules.Ordering.Domain;

namespace GiysiGo.Modules.Ordering.Application
{
    public sealed class CartCommandService(
        ITransactionRunner transactionRunner,
        ICartCommandRepository cartRepository,
        IOrderWorkflowRepository workflowRepository,
        ICustomerAddressReader addressReader,
        IStoreCatalogReader catalogReader,
        ICurrentUser currentUser) : ICartCommandService
    {
        public async Task<Guid> CreateAsync(Guid storeId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, CancellationToken cancellationToken = default)
        {
            ValidatePickupWindow(pickupStartUtc, pickupEndUtc);
            var customerId = RequireCurrentUserId();
            var address = await addressReader.GetOwnedAddressAsync(customerId, addressId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Address is not owned by the customer.");
            if (await catalogReader.GetStoreLocationAsync(storeId, cancellationToken) is null
                || !await catalogReader.ServesLocationAsync(storeId, address.Latitude, address.Longitude, cancellationToken))
                throw new InvalidOperationException("Store is unavailable for the selected address.");
            var cart = new Cart
            {
                Id = Guid.NewGuid(), CustomerId = customerId, StoreId = storeId, AddressId = addressId,
                PickupWindowStart = pickupStartUtc, PickupWindowEnd = pickupEndUtc, Status = "OPEN", Version = 0
            };
            return await transactionRunner.ExecuteAsync((connection, transaction, token) =>
                cartRepository.CreateAsync(cart, connection, transaction, token), cancellationToken);
        }

        public async Task AddItemAsync(CartItem item, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.AddItemAsync(item, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task AddServiceAsync(CartItemService service, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.AddServiceAsync(service, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task RemoveServiceAsync(Guid cartId, Guid cartItemId, Guid serviceId, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.RemoveServiceAsync(cartId, cartItemId, serviceId, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task AddPhotoAsync(CartItemPhoto photo, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            if (string.IsNullOrWhiteSpace(photo.StorageKey) || photo.StorageKey.Length > 1024)
                throw new ArgumentException("Photo storage key must contain 1 to 1024 characters.", nameof(photo));
            if (string.IsNullOrWhiteSpace(photo.PhotoType) || photo.PhotoType.Length > 40)
                throw new ArgumentException("Photo type must contain 1 to 40 characters.", nameof(photo));
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.AddPhotoAsync(photo, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task RemovePhotoAsync(Guid cartId, Guid cartItemId, Guid photoId, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.RemovePhotoAsync(cartId, cartItemId, photoId, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task RemoveItemAsync(Guid cartId, Guid itemId, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                await cartRepository.RemoveItemAsync(cartId, itemId, customerId, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        public async Task SetAddressAndPickupWindowAsync(Guid cartId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, int expectedCartVersion, CancellationToken cancellationToken = default)
        {
            ValidatePickupWindow(pickupStartUtc, pickupEndUtc);
            var customerId = RequireCurrentUserId();
            await transactionRunner.ExecuteAsync(async (connection, transaction, token) =>
            {
                var storeId = await workflowRepository.GetOpenCartStoreIdAsync(cartId, customerId, connection, transaction, token)
                    ?? throw new InvalidOperationException("Open cart was not found for the customer.");
                var address = await addressReader.GetOwnedAddressAsync(customerId, addressId, token)
                    ?? throw new UnauthorizedAccessException("Address is not owned by the customer.");
                if (!await catalogReader.ServesLocationAsync(storeId, address.Latitude, address.Longitude, token))
                    throw new InvalidOperationException("Store does not serve the selected address.");
                await cartRepository.SetAddressAndPickupWindowAsync(cartId, customerId, addressId, pickupStartUtc, pickupEndUtc, expectedCartVersion, connection, transaction, token);
                return true;
            }, cancellationToken);
        }

        private Guid RequireCurrentUserId() => currentUser.IsAuthenticated && currentUser.UserId is Guid userId
            ? userId
            : throw new UnauthorizedAccessException("An authenticated user is required for this operation.");

        private static void ValidatePickupWindow(DateTime? startUtc, DateTime? endUtc)
        {
            if (startUtc.HasValue != endUtc.HasValue)
                throw new ArgumentException("Pickup window start and end must both be provided or both be omitted.");
            if (startUtc is not null && (startUtc.Value.Kind != DateTimeKind.Utc || endUtc!.Value.Kind != DateTimeKind.Utc || endUtc <= startUtc))
                throw new ArgumentException("Pickup window must be a valid UTC interval.");
        }
    }
}
