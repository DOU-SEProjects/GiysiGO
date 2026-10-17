using System.Data.Common;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Application.Interfaces.Repositories
{
    public interface ICustomerAddressRepository
    {
        Task<Guid> InsertAsync(Guid userId, CustomerAddress address, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<CustomerAddress?> GetOwnedAsync(Guid userId, Guid addressId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerAddress>> GetOwnedListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task UpdateOwnedAsync(Guid userId, CustomerAddress address, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task DeleteOwnedAsync(Guid userId, Guid addressId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
