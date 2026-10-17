using System.Data.Common;
using Dapper;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class IdentityAccessRepository : IdentityCommandRepositoryBase, IIdentityAccessRepository
    {
        public Task<Guid> GrantRolePermissionAsync(Guid roleId, Guid permissionId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spGrantRolePermission]", new { RoleId = roleId, PermissionId = permissionId }, connection, transaction, cancellationToken));

        public Task RevokeRolePermissionAsync(Guid roleId, Guid permissionId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spRevokeRolePermission]", new { RoleId = roleId, PermissionId = permissionId }, connection, transaction, cancellationToken));

        public async Task<IReadOnlyList<RolePermission>> GetRolePermissionListAsync(Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var rows = await connection.QueryAsync<RolePermission>(Procedure("[identity].[spGetRolePermissionList]", new { RoleId = roleId }, connection, transaction, cancellationToken));
            return rows.AsList();
        }

        public Task<Guid> GrantUserRoleAsync(Guid userId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spGrantUserRole]", new { UserId = userId, RoleId = roleId }, connection, transaction, cancellationToken));

        public Task RevokeUserRoleAsync(Guid userId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spRevokeUserRole]", new { UserId = userId, RoleId = roleId }, connection, transaction, cancellationToken));

        public async Task<IReadOnlyList<UserRole>> GetUserRoleListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var rows = await connection.QueryAsync<UserRole>(Procedure("[identity].[spGetUserRoleList]", new { UserId = userId }, connection, transaction, cancellationToken));
            return rows.AsList();
        }

        public Task<Guid> GrantCorporateMembershipAsync(Guid userId, Guid corporateId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spGrantCorporateMembership]", new { UserId = userId, CorporateId = corporateId, RoleId = roleId }, connection, transaction, cancellationToken));

        public Task RevokeCorporateMembershipAsync(Guid userId, Guid corporateId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spRevokeCorporateMembership]", new { UserId = userId, CorporateId = corporateId, RoleId = roleId }, connection, transaction, cancellationToken));

        public async Task<IReadOnlyList<CorporateMembership>> GetCorporateMembershipListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var rows = await connection.QueryAsync<CorporateMembership>(Procedure("[identity].[spGetCorporateMembershipList]", new { UserId = userId }, connection, transaction, cancellationToken));
            return rows.AsList();
        }

        public Task<Guid> GrantStoreAssignmentAsync(Guid userId, Guid storeId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.QuerySingleAsync<Guid>(Procedure("[identity].[spGrantStoreAssignment]", new { UserId = userId, StoreId = storeId, RoleId = roleId }, connection, transaction, cancellationToken));

        public Task RevokeStoreAssignmentAsync(Guid userId, Guid storeId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
            => connection.ExecuteAsync(Procedure("[identity].[spRevokeStoreAssignment]", new { UserId = userId, StoreId = storeId, RoleId = roleId }, connection, transaction, cancellationToken));

        public async Task<IReadOnlyList<StoreAssignment>> GetStoreAssignmentListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var rows = await connection.QueryAsync<StoreAssignment>(Procedure("[identity].[spGetStoreAssignmentList]", new { UserId = userId }, connection, transaction, cancellationToken));
            return rows.AsList();
        }
    }
}
