using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Domain;

public class ShipmentTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static TrackingEvent Event(TrackingStatus status, DateTimeOffset timestamp, string description = "Scan") =>
        new(status, description, "Midrand Depot", timestamp);

    private static Shipment CreateShipment(TrackingEvent initialEvent) =>
        new(
            "WB10000001",
            new Receiver("T. Mokoena", "Johannesburg", "+27 82 000 0000", "1 Example Street"),
            new DateOnly(2026, 10, 9),
            initialEvent);

    [Fact]
    public void CurrentStatus_EventsAddedOutOfOrder_UsesLatestTimestamp()
    {
        var shipment = CreateShipment(Event(TrackingStatus.Packed, Start));
        shipment.AddEvent(Event(TrackingStatus.OutForDelivery, Start.AddDays(2)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, Start.AddDays(1)));

        Assert.Equal(TrackingStatus.OutForDelivery, shipment.CurrentStatus);
    }

    [Fact]
    public void LastUpdated_IsTimestampOfNewestEvent()
    {
        var shipment = CreateShipment(Event(TrackingStatus.Packed, Start));
        shipment.AddEvent(Event(TrackingStatus.InTransit, Start.AddHours(5)));
        shipment.AddEvent(Event(TrackingStatus.Processing, Start.AddHours(2)));

        Assert.Equal(Start.AddHours(5), shipment.LastUpdated);
    }

    [Fact]
    public void CurrentEvent_TimestampTie_LaterRecordedEventWins()
    {
        var shipment = CreateShipment(Event(TrackingStatus.Packed, Start));
        shipment.AddEvent(Event(TrackingStatus.InTransit, Start.AddHours(1), "First at this time"));
        shipment.AddEvent(Event(TrackingStatus.OutForDelivery, Start.AddHours(1), "Second at this time"));

        Assert.Equal("Second at this time", shipment.CurrentEvent.Description);
    }

    [Fact]
    public void Events_CannotBeModifiedFromOutside()
    {
        var shipment = CreateShipment(Event(TrackingStatus.Packed, Start));
        var events = Assert.IsAssignableFrom<IList<TrackingEvent>>(shipment.Events);

        Assert.Throws<NotSupportedException>(() => events.Add(Event(TrackingStatus.Delivered, Start.AddDays(3))));
        Assert.Single(shipment.Events);
    }
}
