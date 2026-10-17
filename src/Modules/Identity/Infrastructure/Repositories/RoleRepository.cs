using GiysiGo.BuildingBlocks.Infrastructure;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class RoleRepository(SqlConnectionFactory connectionFactory) : RepositoryBase<Role>(connectionFactory), IRoleRepository
    {
        protected override string SpGetAll => "[identity].[spGetAllRoles]";
        protected override string SpGetById => "[identity].[spGetRoleById]";
        protected override string SpInsert => "[identity].[spInsertRole]";
        protected override string SpUpdate => "[identity].[spUpdateRole]";
        protected override string SpDelete => "[identity].[spDeleteRole]";

        protected override object InsertParameters(Role entity) => new
        {
            Id = entity.Id == Guid.Empty ? (Guid?)null : entity.Id,
            entity.Code,
            entity.Name,
            entity.Scope,
            entity.IsActive
        };

        protected override object UpdateParameters(Role entity) => new
        {
            entity.Id,
            entity.Name,
            entity.IsActive
        };
    }
}
