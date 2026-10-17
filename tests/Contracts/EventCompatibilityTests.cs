using System.Text.Json;
using System.Text.Json.Nodes;
using GiysiGo.BuildingBlocks.Application.Events;
using GiysiGo.Modules.Operations.IntegrationEvents;
using GiysiGo.Modules.Ordering.IntegrationEvents;

namespace GiysiGo.Contracts.Tests
{
    public sealed class EventCompatibilityTests
    {
        private static readonly Dictionary<string, Type> EventTypes = new()
        {
            ["ordering.order_placed.v1"] = typeof(OrderPlacedEvent),
            ["ordering.order_ready_for_delivery.v1"] = typeof(OrderReadyForDeliveryEvent),
            ["ordering.payment_succeeded.v1"] = typeof(PaymentSucceededEvent),
            ["ordering.quote_rejected.v1"] = typeof(QuoteRejectedEvent),
            ["operations.pickup_at_store.v1"] = typeof(PickupAtStoreEvent),
            ["operations.all_items_inspected.v1"] = typeof(AllItemsInspectedEvent),
            ["operations.all_work_completed.v1"] = typeof(AllWorkCompletedEvent),
            ["operations.delivery_completed.v1"] = typeof(DeliveryCompletedEvent),
            ["operations.delivery_task_assigned.v1"] = typeof(DeliveryTaskAssignedEvent),
            ["operations.delivery_task_started.v1"] = typeof(DeliveryTaskStartedEvent),
            ["operations.return_completed.v1"] = typeof(ReturnCompletedEvent)
        };

        public static IEnumerable<object[]> WireFixtures()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "events-v1.json")));
            return document.RootElement.EnumerateArray()
                .Select(value => new object[] { value.GetProperty("EventType").GetString()!, value.GetRawText() })
                .ToArray();
        }

        [Theory]
        [MemberData(nameof(WireFixtures))]
        public void Stored_v1_payloads_round_trip_without_changing_shape(string eventType, string payload)
        {
            var type = EventTypes[eventType];
            var integrationEvent = Assert.IsAssignableFrom<IIntegrationEvent>(JsonSerializer.Deserialize(payload, type));

            Assert.Equal(eventType, integrationEvent.EventType);
            Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
            Assert.Equal(DateTimeKind.Utc, integrationEvent.OccurredAtUtc.Kind);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(payload), JsonNode.Parse(JsonSerializer.Serialize(integrationEvent, type))));
        }

        [Fact]
        public void Delivery_and_return_have_distinct_wire_types_for_the_same_order_and_task()
        {
            var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
            var order = Guid.NewGuid();
            var task = Guid.NewGuid();
            IIntegrationEvent delivery = new DeliveryCompletedEvent(Guid.NewGuid(), now, order, task);
            IIntegrationEvent returned = new ReturnCompletedEvent(Guid.NewGuid(), now, order, task);

            Assert.NotEqual(delivery.EventType, returned.EventType);
            Assert.Equal("operations.return_completed.v1", returned.EventType);
        }
    }
}
