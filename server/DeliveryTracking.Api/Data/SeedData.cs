using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Data;

/// <summary>
/// The six demo shipments, one at each status. Every timestamp is an offset
/// from ReferenceDate, so the whole history can be moved by changing one value.
/// </summary>
public static class SeedData
{
    public static readonly DateTimeOffset ReferenceDate = new(2026, 10, 8, 8, 0, 0, TimeSpan.FromHours(2));

    // A method rather than a static list, because Shipment.AddEvent mutates a
    // shipment and each caller (seeder, tests) needs its own copies.
    public static IReadOnlyList<Shipment> CreateShipments() =>
    [
        PackedShipment(),
        ProcessingShipment(),
        InTransitShipment(),
        OutForDeliveryShipment(),
        DeliveredShipment(),
        ExceptionShipment(),
    ];

    private static Shipment PackedShipment()
    {
        return new Shipment(
            "WB10000001",
            new Receiver("L. Naidoo", "Durban", "+27 82 555 0101", "14 Florida Road, Morningside"),
            new DateOnly(2026, 10, 12),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Cape Town Warehouse", TimeSpan.FromHours(2)));
    }

    private static Shipment ProcessingShipment()
    {
        var shipment = new Shipment(
            "WB10000002",
            new Receiver("S. van der Merwe", "Pretoria", "+27 83 555 0102", "220 Lynnwood Road, Brooklyn"),
            new DateOnly(2026, 10, 11),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Cape Town Warehouse", TimeSpan.FromHours(27)));
        shipment.AddEvent(Event(TrackingStatus.Processing, "Parcel received at sorting hub", "Cape Town Hub", TimeSpan.FromHours(20)));
        return shipment;
    }

    private static Shipment InTransitShipment()
    {
        var shipment = new Shipment(
            "WB10000003",
            new Receiver("N. Dlamini", "Bloemfontein", "+27 84 555 0103", "8 Kellner Street, Westdene"),
            new DateOnly(2026, 10, 10),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Durban Warehouse", TimeSpan.FromHours(48)));
        shipment.AddEvent(Event(TrackingStatus.Processing, "Parcel received at sorting hub", "Durban Hub", TimeSpan.FromHours(42)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Departed sorting hub", "Durban Hub", TimeSpan.FromHours(34)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Arrived at regional hub", "Bloemfontein Hub", TimeSpan.FromHours(3)));
        return shipment;
    }

    // Matches the example response in docs/api-contract.md.
    private static Shipment OutForDeliveryShipment()
    {
        var shipment = new Shipment(
            "WB10000004",
            new Receiver("T. Mokoena", "Johannesburg", "+27 82 555 0104", "31 Jan Smuts Avenue, Parktown"),
            new DateOnly(2026, 10, 9),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Cape Town Warehouse", TimeSpan.FromHours(52)));
        shipment.AddEvent(Event(TrackingStatus.Processing, "Parcel received at sorting hub", "Cape Town Hub", TimeSpan.FromHours(48)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Departed sorting hub", "Cape Town Hub", TimeSpan.FromHours(44)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Arrived at regional hub", "Johannesburg Hub", TimeSpan.FromHours(6)));
        shipment.AddEvent(Event(TrackingStatus.OutForDelivery, "Parcel out for delivery", "Midrand Depot", TimeSpan.FromMinutes(18)));
        return shipment;
    }

    private static Shipment DeliveredShipment()
    {
        var shipment = new Shipment(
            "WB10000005",
            new Receiver("A. Botha", "Gqeberha", "+27 72 555 0105", "5 Marine Drive, Summerstrand"),
            new DateOnly(2026, 10, 7),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Johannesburg Warehouse", TimeSpan.FromHours(96)));
        shipment.AddEvent(Event(TrackingStatus.Processing, "Parcel received at sorting hub", "Johannesburg Hub", TimeSpan.FromHours(90)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Departed sorting hub", "Johannesburg Hub", TimeSpan.FromHours(80)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Arrived at regional hub", "Gqeberha Hub", TimeSpan.FromHours(40)));
        shipment.AddEvent(Event(TrackingStatus.OutForDelivery, "Parcel out for delivery", "Gqeberha Depot", TimeSpan.FromHours(29)));
        shipment.AddEvent(Event(TrackingStatus.Delivered, "Delivered and signed for", "Gqeberha", TimeSpan.FromHours(26)));
        return shipment;
    }

    private static Shipment ExceptionShipment()
    {
        var shipment = new Shipment(
            "WB10000006",
            new Receiver("K. Pillay", "Polokwane", "+27 76 555 0106", "17 Thabo Mbeki Street, Polokwane Central"),
            new DateOnly(2026, 10, 8),
            Event(TrackingStatus.Packed, "Parcel packed by sender", "Johannesburg Warehouse", TimeSpan.FromHours(50)));
        shipment.AddEvent(Event(TrackingStatus.Processing, "Parcel received at sorting hub", "Johannesburg Hub", TimeSpan.FromHours(45)));
        shipment.AddEvent(Event(TrackingStatus.InTransit, "Departed sorting hub", "Johannesburg Hub", TimeSpan.FromHours(30)));
        shipment.AddEvent(Event(TrackingStatus.OutForDelivery, "Parcel out for delivery", "Polokwane Depot", TimeSpan.FromHours(5)));
        shipment.AddEvent(Event(
            TrackingStatus.Exception, "Delivery attempt failed", "Polokwane", TimeSpan.FromHours(2), "Recipient not available"));
        return shipment;
    }

    private static TrackingEvent Event(
        TrackingStatus status, string description, string location, TimeSpan beforeReference, string? exceptionReason = null) =>
        new(status, description, location, ReferenceDate - beforeReference, exceptionReason);
}
