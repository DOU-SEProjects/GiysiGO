using System.Data;
using Dapper;
using GiysiGo.BuildingBlocks.Infrastructure;
using GiysiGo.Modules.Catalog.Application.Repositories;
using GiysiGo.Modules.Catalog.Domain;

namespace GiysiGo.Modules.Catalog.Infrastructure.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly SqlConnectionFactory _sqlConnectionFactory;

    public CatalogRepository(SqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    // Kategoriler
    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetCategories",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<ProductCategory>(command);
        return result.AsList();
    }

    public async Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetCategoryById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ProductCategory>(command);
    }

    // Ürün Tipleri
    public async Task<IReadOnlyList<ProductType>> GetProductTypesAsync(Guid? categoryId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@CategoryId", categoryId, DbType.Guid);
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypes",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<ProductType>(command);
        return result.AsList();
    }

    public async Task<ProductType?> GetProductTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypeById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ProductType>(command);
    }

    // Sayfalamalı Ürün Listesi
    public async Task<IReadOnlyList<ProductType>> GetProductTypesPagedAsync(Guid? categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@CategoryId", categoryId, DbType.Guid);
        parameters.Add("@PageNumber", pageNumber, DbType.Int32);
        parameters.Add("@PageSize", pageSize, DbType.Int32);
        parameters.Add("@IncludeInactive", false, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypesPaged",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<ProductType>(command);
        return result.AsList();
    }

    // Hizmetler
    public async Task<IReadOnlyList<Service>> GetServicesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetServices",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<Service>(command);
        return result.AsList();
    }

    public async Task<Service?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetServiceById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Service>(command);
    }

    // Dükkanlar
    public async Task<IReadOnlyList<Store>> GetStoresAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStores",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<Store>(command);
        return result.AsList();
    }

    public async Task<Store?> GetStoreByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Store>(command);
    }

    // Dükkan Teklifleri
    public async Task<IReadOnlyList<StoreOffering>> GetStoreOfferingsAsync(Guid? storeId = null, Guid? productTypeServiceId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@StoreId", storeId, DbType.Guid);
        parameters.Add("@ProductTypeServiceId", productTypeServiceId, DbType.Guid);
        parameters.Add("@IncludeInactive", false, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreOfferings",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<StoreOffering>(command);
        return result.AsList();
    }

    public async Task<StoreOffering?> GetStoreOfferingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreOfferingById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<StoreOffering>(command);
    }

    // Ekleme Metotları (GUID Dönenler)
    public async Task<Guid> AddCategoryAsync(string name, string? description, int displayOrder = 0, CancellationToken cancellationToken = default)
    {
        await using var connection = await _sqlConnectionFactory.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Name", name, DbType.String);
        parameters.Add("@Description", description, DbType.String);
        parameters.Add("@DisplayOrder", displayOrder, DbType.Int32);

        var command = new CommandDefinition(
            commandText: "catalog.usp_CreateCategory",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<Guid>(command);
    }
}