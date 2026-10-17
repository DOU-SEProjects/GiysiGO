using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Identity.Domain.Entities
{
    public sealed class RolePermission : EntityBase<Guid>
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
    }
}
