using System.Data.Common;
using Dapper;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class CustomerAddressRepository : IdentityCommandRepositoryBase, ICustomerAddressRepository
    {
        public Task<Guid> InsertAsync(Guid userId, CustomerAddress address, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spInsertCustomerAddress]", new { Id = address.Id == Guid.Empty ? (Guid?)null : address.Id, UserId = userId, address.Label, address.AddressText, address.Latitude, address.Longitude, address.ContactName, address.ContactPhone, address.IsActive }, connection, transaction, cancellationToken));

        public Task<CustomerAddress?> GetOwnedAsync(Guid userId, Guid addressId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleOrDefaultAsync<CustomerAddress>(Procedure("[identity].[spGetOwnedCustomerAddress]", new { UserId = userId, AddressId = addressId }, connection, transaction, cancellationToken));

        public async Task<IReadOnlyList<CustomerAddress>> GetOwnedListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var rows = await connection.QueryAsync<CustomerAddress>(Procedure("[identity].[spGetCustomerAddressesByUser]", new { UserId = userId }, connection, transaction, cancellationToken));
            return rows.AsList();
        }

        public Task UpdateOwnedAsync(Guid userId, CustomerAddress address, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spUpdateOwnedCustomerAddress]", new { UserId = userId, AddressId = address.Id, address.Label, address.AddressText, address.Latitude, address.Longitude, address.ContactName, address.ContactPhone, address.IsActive }, connection, transaction, cancellationToken));

        public Task DeleteOwnedAsync(Guid userId, Guid addressId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spDeleteOwnedCustomerAddress]", new { UserId = userId, AddressId = addressId }, connection, transaction, cancellationToken));
    }
}
