using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Data.Mongo;

public sealed class TrackingEventDocument
{
    public TrackingStatus Status { get; set; }
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";

    /// <summary>Always UTC. The mapper converts to and from DateTimeOffset.</summary>
    public DateTime Timestamp { get; set; }

    public string? ExceptionReason { get; set; }
}
