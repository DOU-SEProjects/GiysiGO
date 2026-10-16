using System.Data.Common;
using GiysiGo.Modules.Ordering.Domain;

namespace GiysiGo.Modules.Ordering.Application.Contracts
{
    /// <summary>Cart editing commands. Every method participates in the caller's transaction.</summary>
    public interface ICartCommandRepository
    {
        Task<Guid> CreateAsync(Cart cart, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task AddItemAsync(CartItem item, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task AddServiceAsync(CartItemService service, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RemoveServiceAsync(Guid cartId, Guid cartItemId, Guid serviceId, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task AddPhotoAsync(CartItemPhoto photo, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RemovePhotoAsync(Guid cartId, Guid cartItemId, Guid photoId, Guid customerId, int expectedCartVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid cartId, Guid itemId, Guid customerId, int expectedVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task SetAddressAndPickupWindowAsync(Guid cartId, Guid customerId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, int expectedVersion, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
