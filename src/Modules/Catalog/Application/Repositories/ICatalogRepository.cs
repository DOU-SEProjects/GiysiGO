using GiysiGo.Modules.Catalog.Domain;

namespace GiysiGo.Modules.Catalog.Application.Repositories;

public interface ICatalogRepository
{
    // 1. Kategoriler
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // 2. Ürün Tipleri (Kategoriye göre filtrelenebilir)
    Task<IReadOnlyList<ProductType>> GetProductTypesAsync(Guid? categoryId = null, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductType?> GetProductTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // 3. Hizmetler (Kuru Temizleme, Ütü vs.)
    Task<IReadOnlyList<Service>> GetServicesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Service?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // 4. Dükkanlar
    Task<IReadOnlyList<Store>> GetStoresAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Store?> GetStoreByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // 5. Dükkan Teklifleri (Fiyatlar - Dükkana veya Ürüne göre filtrelenebilir)
    Task<IReadOnlyList<StoreOffering>> GetStoreOfferingsAsync(Guid? storeId = null, Guid? productTypeServiceId = null, CancellationToken cancellationToken = default);
    Task<StoreOffering?> GetStoreOfferingByIdAsync(Guid id, CancellationToken cancellationToken = default);
}