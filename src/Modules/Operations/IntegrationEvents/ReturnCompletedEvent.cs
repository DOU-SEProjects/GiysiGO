using GiysiGo.BuildingBlocks.Application.Events;

namespace GiysiGo.Modules.Operations.IntegrationEvents
{
    public sealed record ReturnCompletedEvent(Guid EventId, DateTime OccurredAtUtc, Guid OrderId, Guid TaskId) : IIntegrationEvent
    {
        public string EventType => "operations.return_completed.v1";
    }
}
