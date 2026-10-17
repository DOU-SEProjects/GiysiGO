namespace GiysiGo.Modules.Catalog.Domain;

public sealed class StoreOffering
{
    public Guid Id { get; init; }
    public Guid StoreId { get; init; }
    public Guid ProductTypeServiceId { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "TRY";
    public int LeadTimeHours { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreateDate { get; init; }
    public DateTime? UpdateDate { get; init; }
    public DateTime? DeleteDate { get; init; }
}