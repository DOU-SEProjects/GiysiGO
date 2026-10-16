using GiysiGo.Modules.Ordering.Domain;

namespace GiysiGo.Modules.Ordering.Application.Contracts
{
    public interface ICartCommandService
    {
        Task<Guid> CreateAsync(Guid storeId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, CancellationToken cancellationToken = default);
        Task AddItemAsync(CartItem item, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task AddServiceAsync(CartItemService service, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task RemoveServiceAsync(Guid cartId, Guid cartItemId, Guid serviceId, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task AddPhotoAsync(CartItemPhoto photo, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task RemovePhotoAsync(Guid cartId, Guid cartItemId, Guid photoId, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid cartId, Guid itemId, int expectedCartVersion, CancellationToken cancellationToken = default);
        Task SetAddressAndPickupWindowAsync(Guid cartId, Guid addressId, DateTime? pickupStartUtc, DateTime? pickupEndUtc, int expectedCartVersion, CancellationToken cancellationToken = default);
    }
}
