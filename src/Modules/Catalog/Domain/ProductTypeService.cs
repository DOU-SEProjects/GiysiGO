namespace GiysiGo.Modules.Catalog.Domain;

public sealed class ProductTypeService
{
    public Guid Id { get; init; }
    public Guid ProductTypeId { get; init; }
    public Guid ServiceId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreateDate { get; init; }
    public DateTime? DeleteDate { get; init; }
}