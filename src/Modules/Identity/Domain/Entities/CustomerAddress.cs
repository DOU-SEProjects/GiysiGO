using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Identity.Domain.Entities
{
    public sealed class CustomerAddress : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string AddressText { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
