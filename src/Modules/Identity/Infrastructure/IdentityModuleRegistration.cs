using Microsoft.Extensions.DependencyInjection;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Infrastructure.Repositories;

namespace GiysiGo.Modules.Identity.Infrastructure
{
    public static class IdentityModuleRegistration
    {
        public static IServiceCollection AddIdentityModule(this IServiceCollection services)
        {
            services.AddScoped<ICorporateRepository, CorporateRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
            services.AddScoped<IIdentityAccessRepository, IdentityAccessRepository>();
            return services;
        }
    }
}
