namespace DeliveryTracking.Api.Domain;

public sealed class Shipment
{
    private readonly List<TrackingEvent> _events;

    public Shipment(string waybill, Receiver receiver, DateOnly estimatedDelivery, TrackingEvent initialEvent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(waybill);
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(initialEvent);

        Waybill = waybill;
        Receiver = receiver;
        EstimatedDelivery = estimatedDelivery;
        _events = [initialEvent];
        Events = _events.AsReadOnly();
    }

    public string Waybill { get; }
    public Receiver Receiver { get; }
    public DateOnly EstimatedDelivery { get; }

    /// <summary>In the order the events were recorded, not sorted by timestamp.</summary>
    /// <remarks>
    /// A read-only wrapper, so callers can't cast it back to the list and add to it.
    /// </remarks>
    public IReadOnlyList<TrackingEvent> Events { get; }

    /// <summary>
    /// The event with the latest timestamp. On a tie, the one recorded later wins.
    /// </summary>
    public TrackingEvent CurrentEvent
    {
        get
        {
            var newest = _events[0];
            foreach (var trackingEvent in _events)
            {
                // >= rather than > so that a later-recorded event wins a tie.
                if (trackingEvent.Timestamp >= newest.Timestamp)
                {
                    newest = trackingEvent;
                }
            }

            return newest;
        }
    }

    /// <summary>Derived from CurrentEvent; never stored.</summary>
    public TrackingStatus CurrentStatus => CurrentEvent.Status;

    /// <summary>The timestamp of CurrentEvent.</summary>
    public DateTimeOffset LastUpdated => CurrentEvent.Timestamp;

    public void AddEvent(TrackingEvent trackingEvent)
    {
        ArgumentNullException.ThrowIfNull(trackingEvent);
        _events.Add(trackingEvent);
    }
}
