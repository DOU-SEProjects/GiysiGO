using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Identity.Domain.Entities
{
    public sealed class Role : EntityBase<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
