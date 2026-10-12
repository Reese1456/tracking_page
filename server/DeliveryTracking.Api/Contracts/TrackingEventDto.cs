using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Contracts;

/// <param name="Timestamp">In UTC.</param>
public sealed record TrackingEventDto(
    TrackingStatus Status,
    string Description,
    string Location,
    DateTimeOffset Timestamp);
