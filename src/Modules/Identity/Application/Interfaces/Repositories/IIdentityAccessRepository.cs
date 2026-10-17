using System.Data.Common;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Application.Interfaces.Repositories
{
    public interface IIdentityAccessRepository
    {
        Task<Guid> GrantRolePermissionAsync(Guid roleId, Guid permissionId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RevokeRolePermissionAsync(Guid roleId, Guid permissionId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RolePermission>> GetRolePermissionListAsync(Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid> GrantUserRoleAsync(Guid userId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RevokeUserRoleAsync(Guid userId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserRole>> GetUserRoleListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid> GrantCorporateMembershipAsync(Guid userId, Guid corporateId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RevokeCorporateMembershipAsync(Guid userId, Guid corporateId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CorporateMembership>> GetCorporateMembershipListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<Guid> GrantStoreAssignmentAsync(Guid userId, Guid storeId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task RevokeStoreAssignmentAsync(Guid userId, Guid storeId, Guid roleId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StoreAssignment>> GetStoreAssignmentListAsync(Guid userId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
