using GiysiGo.BuildingBlocks.Infrastructure;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class PermissionRepository(SqlConnectionFactory connectionFactory) : RepositoryBase<Permission>(connectionFactory), IPermissionRepository
    {
        protected override string SpGetAll => "[identity].[spGetAllPermissions]";
        protected override string SpGetById => "[identity].[spGetPermissionById]";
        protected override string SpInsert => "[identity].[spInsertPermission]";
        protected override string SpUpdate => "[identity].[spUpdatePermission]";
        protected override string SpDelete => "[identity].[spDeletePermission]";

        protected override object InsertParameters(Permission entity) => new
        {
            Id = entity.Id == Guid.Empty ? (Guid?)null : entity.Id,
            entity.Code,
            entity.Description,
            entity.IsActive
        };

        protected override object UpdateParameters(Permission entity) => new
        {
            entity.Id,
            entity.Description,
            entity.IsActive
        };
    }
}
