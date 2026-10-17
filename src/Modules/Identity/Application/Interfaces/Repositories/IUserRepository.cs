using System.Data.Common;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<Guid> InsertAsync(User user, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task SetPasswordHashAsync(Guid userId, string passwordHash, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task SetActiveAsync(Guid userId, bool isActive, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
