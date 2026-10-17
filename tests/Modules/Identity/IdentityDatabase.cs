using Dapper;
using GiysiGo.BuildingBlocks.Infrastructure;
using GiysiGo.Modules.Identity.Infrastructure;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace GiysiGo.Modules.Identity.Tests
{
    public sealed class IdentityDatabase : IDisposable
    {
        private readonly ServiceProvider provider;
        private readonly IServiceScope scope;

        public IdentityDatabase()
        {
            var configuration = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GIYSIGO_IDENTITY_TEST_CONNECTION"));
            if (!configuration.InitialCatalog.StartsWith("GiysiGo_IdentityTest_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Only isolated GiysiGo_IdentityTest_ databases are allowed.");
            }
            Factory = new SqlConnectionFactory(configuration.ConnectionString);
            Transactions = new SqlTransactionRunner(Factory);
            var services = new ServiceCollection();
            services.AddSingleton(Factory);
            services.AddIdentityModule();
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            scope = provider.CreateScope();
        }

        public SqlConnectionFactory Factory { get; }
        public SqlTransactionRunner Transactions { get; }
        public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

        public Task<Guid> CreateUserAsync(string? email = null) => Transactions.ExecuteAsync((connection, transaction, token) =>
            Get<IUserRepository>().InsertAsync(new User { Email = email ?? $"{Guid.NewGuid():N}@example.test", PasswordHash = "test-only-placeholder-hash" }, connection, transaction, token));

        public Task<Guid> CreateRoleAsync(string roleScope) => Get<IRoleRepository>().InsertAsync(new Role
        {
            Code = Guid.NewGuid().ToString("N"), Name = "Test role", Scope = roleScope
        });

        public Task<Guid> CreateStoreAsync(Guid corporateId) => Transactions.ExecuteAsync((connection, transaction, token) =>
            connection.QuerySingleAsync<Guid>(new CommandDefinition(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "InsertStore.sql")),
                new { CorporateId = corporateId }, transaction, cancellationToken: token)));

        public void Dispose()
        {
            scope.Dispose();
            provider.Dispose();
        }
    }
}
