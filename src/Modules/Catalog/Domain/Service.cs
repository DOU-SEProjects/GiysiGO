namespace GiysiGo.Modules.Catalog.Domain;

public sealed class Service
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreateDate { get; init; }
    public DateTime? UpdateDate { get; init; }
    public DateTime? DeleteDate { get; init; }
}