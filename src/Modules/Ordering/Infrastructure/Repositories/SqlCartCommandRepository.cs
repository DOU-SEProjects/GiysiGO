using System.Data;
using System.Data.Common;
using Dapper;
using GiysiGo.Modules.Ordering.Application.Contracts;
using GiysiGo.Modules.Ordering.Domain;

namespace GiysiGo.Modules.Ordering.Infrastructure.Repositories
{
    public sealed class SqlCartCommandRepository : ICartCommandRepository
    {
        public async Task<Guid> CreateAsync(Cart cart, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.QuerySingleAsync<Guid>(new CommandDefinition("[ordering].[spCreateCart]", new
            {
                cart.Id, cart.CustomerId, cart.StoreId, cart.AddressId,
                cart.PickupWindowStart, cart.PickupWindowEnd
            }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task AddItemAsync(CartItem item, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spAddCartItem]", new
            {
                item.Id, item.CartId, CustomerId = customerId, item.ProductTypeId, item.CustomerNote, item.CustomerMeasurement, ExpectedCartVersion = expectedCartVersion
            }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task AddServiceAsync(CartItemService service, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spAddCartItemService]", new { service.Id, service.CartItemId, service.StoreOfferingId, CustomerId = customerId, ExpectedCartVersion = expectedCartVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task RemoveServiceAsync(Guid cartId, Guid cartItemId, Guid serviceId, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spRemoveCartItemService]", new { CartId = cartId, CartItemId = cartItemId, ServiceId = serviceId, CustomerId = customerId, ExpectedCartVersion = expectedCartVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task AddPhotoAsync(CartItemPhoto photo, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spAddCartItemPhoto]", new { photo.Id, photo.CartItemId, photo.StorageKey, photo.PhotoType, CustomerId = customerId, ExpectedCartVersion = expectedCartVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task RemovePhotoAsync(Guid cartId, Guid cartItemId, Guid photoId, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spRemoveCartItemPhoto]", new { CartId = cartId, CartItemId = cartItemId, PhotoId = photoId, CustomerId = customerId, ExpectedCartVersion = expectedCartVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task RemoveItemAsync(Guid cartId, Guid itemId, Guid customerId, int expectedVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spRemoveCartItem]", new { CartId = cartId, ItemId = itemId, CustomerId = customerId, ExpectedVersion = expectedVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        public async Task SetAddressAndPickupWindowAsync(Guid cartId, Guid customerId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, int expectedVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default) =>
            await connection.ExecuteAsync(new CommandDefinition("[ordering].[spUpdateCartDeliveryDetails]", new { CartId = cartId, CustomerId = customerId, AddressId = addressId, PickupWindowStart = pickupStartUtc, PickupWindowEnd = pickupEndUtc, ExpectedVersion = expectedVersion }, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }
}
