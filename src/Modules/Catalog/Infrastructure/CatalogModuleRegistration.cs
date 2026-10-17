using GiysiGo.Modules.Catalog.Application.Repositories;
using GiysiGo.Modules.Catalog.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GiysiGo.Modules.Catalog.Infrastructure;

public static class CatalogModuleRegistration
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        return services;
    }
}