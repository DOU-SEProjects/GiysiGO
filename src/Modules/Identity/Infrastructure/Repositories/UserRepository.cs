using System.Data.Common;
using Dapper;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class UserRepository : IdentityCommandRepositoryBase, IUserRepository
    {
        public Task<Guid> InsertAsync(User user, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spInsertUser]", new { Id = user.Id == Guid.Empty ? (Guid?)null : user.Id, user.Email, user.PasswordHash, user.IsActive }, connection, transaction, cancellationToken));

        public Task<User?> GetByIdAsync(Guid id, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleOrDefaultAsync<User>(Procedure("[identity].[spGetUserById]", new { Id = id }, connection, transaction, cancellationToken));

        public Task<User?> GetByEmailAsync(string email, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleOrDefaultAsync<User>(Procedure("[identity].[spGetUserByEmail]", new { Email = email }, connection, transaction, cancellationToken));

        public Task SetPasswordHashAsync(Guid userId, string passwordHash, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spSetUserPasswordHash]", new { UserId = userId, PasswordHash = passwordHash }, connection, transaction, cancellationToken));

        public Task SetActiveAsync(Guid userId, bool isActive, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spSetUserActive]", new { UserId = userId, IsActive = isActive }, connection, transaction, cancellationToken));

        public Task DeleteAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spDeleteUser]", new { UserId = userId }, connection, transaction, cancellationToken));
    }
}
