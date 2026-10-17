using GiysiGo.BuildingBlocks.Application.Events;

namespace GiysiGo.Modules.Operations.IntegrationEvents
{
    public sealed record DeliveryTaskAssignedEvent(Guid EventId, DateTime OccurredAtUtc, Guid OrderId, Guid TaskId, string TaskType) : IIntegrationEvent
    {
        public string EventType => "operations.delivery_task_assigned.v1";
    }
}
