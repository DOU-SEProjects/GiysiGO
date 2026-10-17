namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public sealed record StoreScopeSnapshot(Guid StoreId, Guid CorporateId, bool IsActive);
}
