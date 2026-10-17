using GiysiGo.Modules.Catalog.Domain;

namespace GiysiGo.Modules.Catalog.Application.Repositories;

public interface ICatalogRepository
{
    // Kategoriler
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Ürün Tipleri
    Task<IReadOnlyList<ProductType>> GetProductTypesAsync(Guid? categoryId = null, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductType?> GetProductTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Sayfalamalı Ürün Listesi
    Task<IReadOnlyList<ProductType>> GetProductTypesPagedAsync(Guid? categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Hizmetler
    Task<IReadOnlyList<Service>> GetServicesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Service?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Dükkanlar
    Task<IReadOnlyList<Store>> GetStoresAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Store?> GetStoreByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Dükkan Teklifleri
    Task<IReadOnlyList<StoreOffering>> GetStoreOfferingsAsync(Guid? storeId = null, Guid? productTypeServiceId = null, CancellationToken cancellationToken = default);
    Task<StoreOffering?> GetStoreOfferingByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Ekleme Metotları (GUID Dönenler)
    Task<Guid> AddCategoryAsync(string name, string? description, int displayOrder = 0, CancellationToken cancellationToken = default);
}