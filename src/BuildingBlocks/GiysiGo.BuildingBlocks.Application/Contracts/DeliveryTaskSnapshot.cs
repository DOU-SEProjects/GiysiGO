namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public sealed record DeliveryTaskSnapshot(Guid TaskId, Guid OrderId, string TaskType, string Status);
}
