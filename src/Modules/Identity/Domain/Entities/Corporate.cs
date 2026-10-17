using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Identity.Domain.Entities
{
    public sealed class Corporate : EntityBase<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
