using GiysiGo.BuildingBlocks.Domain;

namespace GiysiGo.Modules.Ordering.Domain
{
    /// <summary>Maps to the ordering.OrderItem table. SQL datetime2 values are UTC by convention.</summary>
    public sealed class OrderItem : EntityBase<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid ProductTypeId { get; set; }
        public string ProductTypeNameSnapshot { get; set; } = string.Empty;
        public string? CustomerNote { get; set; }
        public decimal? CustomerMeasurement { get; set; }
        public decimal? VerifiedMeasurement { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string InspectionStatus { get; set; } = "PENDING";
    }
}
