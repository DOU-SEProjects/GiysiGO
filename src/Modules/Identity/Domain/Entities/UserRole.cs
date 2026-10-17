using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Identity.Domain.Entities
{
    public sealed class UserRole : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleScope { get; set; } = string.Empty;
    }
}
