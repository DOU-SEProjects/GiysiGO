using System.Data;
using Dapper;
using GiysiGo.Modules.Catalog.Application.Repositories;
using GiysiGo.Modules.Catalog.Domain;

namespace GiysiGo.Modules.Catalog.Infrastructure.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly IDbConnection _dbConnection;

    public CatalogRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetCategories",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<ProductCategory>(command);
        return result.AsList();
    }

    public async Task<ProductCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetCategoryById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.QuerySingleOrDefaultAsync<ProductCategory>(command);
    }

    public async Task<IReadOnlyList<ProductType>> GetProductTypesAsync(Guid? categoryId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CategoryId", categoryId, DbType.Guid);
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypes",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<ProductType>(command);
        return result.AsList();
    }

    public async Task<ProductType?> GetProductTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypeById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.QuerySingleOrDefaultAsync<ProductType>(command);
    }

    public async Task<IReadOnlyList<Service>> GetServicesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetServices",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<Service>(command);
        return result.AsList();
    }

    public async Task<Service?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetServiceById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.QuerySingleOrDefaultAsync<Service>(command);
    }

    public async Task<IReadOnlyList<Store>> GetStoresAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStores",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<Store>(command);
        return result.AsList();
    }

    public async Task<Store?> GetStoreByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.QuerySingleOrDefaultAsync<Store>(command);
    }

    public async Task<IReadOnlyList<StoreOffering>> GetStoreOfferingsAsync(Guid? storeId = null, Guid? productTypeServiceId = null, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StoreId", storeId, DbType.Guid);
        parameters.Add("@ProductTypeServiceId", productTypeServiceId, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreOfferings",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<StoreOffering>(command);
        return result.AsList();
    }

    public async Task<StoreOffering?> GetStoreOfferingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetStoreOfferingById",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.QuerySingleOrDefaultAsync<StoreOffering>(command);
    }
    public async Task<IReadOnlyList<ProductType>> GetProductTypesPagedAsync(Guid? categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CategoryId", categoryId, DbType.Guid);
        parameters.Add("@PageNumber", pageNumber, DbType.Int32);
        parameters.Add("@PageSize", pageSize, DbType.Int32);

        var command = new CommandDefinition(
            commandText: "catalog.usp_GetProductTypesPaged",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result = await _dbConnection.QueryAsync<ProductType>(command);
        return result.AsList();
    }

    public async Task<Guid> AddCategoryAsync(string name, string? description, int displayOrder = 0, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Name", name, DbType.String);
        parameters.Add("@Description", description, DbType.String);
        parameters.Add("@DisplayOrder", displayOrder, DbType.Int32);

        var command = new CommandDefinition(
            commandText: "catalog.usp_InsertCategory",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await _dbConnection.ExecuteScalarAsync<Guid>(command);
    }
}