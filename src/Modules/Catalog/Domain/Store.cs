namespace GiysiGo.Modules.Catalog.Domain;

public sealed class Store
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreateDate { get; init; }
    public DateTime? UpdateDate { get; init; }
    public DateTime? DeleteDate { get; init; }
}