namespace DeliveryTracking.Api.Domain;

public sealed class TrackingEvent
{
    public TrackingEvent(
        TrackingStatus status,
        string description,
        string location,
        DateTimeOffset timestamp,
        string? exceptionReason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (status == TrackingStatus.Exception && string.IsNullOrWhiteSpace(exceptionReason))
        {
            throw new ArgumentException("An Exception event needs a reason.", nameof(exceptionReason));
        }

        if (status != TrackingStatus.Exception && exceptionReason is not null)
        {
            throw new ArgumentException("Only an Exception event can have a reason.", nameof(exceptionReason));
        }

        Status = status;
        Description = description;
        Location = location;
        Timestamp = timestamp;
        ExceptionReason = exceptionReason;
    }

    public TrackingStatus Status { get; }
    public string Description { get; }
    public string Location { get; }
    public DateTimeOffset Timestamp { get; }

    /// <summary>Set only when Status is Exception, and never blank then.</summary>
    public string? ExceptionReason { get; }
}
