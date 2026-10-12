using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Domain;
using MongoDB.Bson;

namespace DeliveryTracking.Api.Tests.Data;

public class ShipmentDocumentMapperTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTime CreatedAt = new(2026, 10, 6, 7, 10, 0, DateTimeKind.Utc);

    private static Shipment CreateShipment(TrackingEvent initialEvent) =>
        new(
            "WB10000006",
            new Receiver("T. Mokoena", "Johannesburg", "+27 82 000 0000", "1 Example Street"),
            new DateOnly(2026, 10, 9),
            initialEvent);

    private static Shipment RoundTrip(Shipment shipment) =>
        ShipmentDocumentMapper.ToDomain(ShipmentDocumentMapper.ToDocument(shipment, CreatedAt));

    [Fact]
    public void RoundTrip_KeepsWaybillReceiverAndEstimatedDelivery()
    {
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Cape Town Hub", Start));

        var result = RoundTrip(shipment);

        Assert.Equal("WB10000006", result.Waybill);
        Assert.Equal("T. Mokoena", result.Receiver.Name);
        Assert.Equal("Johannesburg", result.Receiver.City);
        Assert.Equal("+27 82 000 0000", result.Receiver.Phone);
        Assert.Equal("1 Example Street", result.Receiver.StreetAddress);
        Assert.Equal(new DateOnly(2026, 10, 9), result.EstimatedDelivery);
    }

    [Fact]
    public void RoundTrip_KeepsEventsInRecordedOrderAndCurrentStatus()
    {
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Cape Town Hub", Start));
        shipment.AddEvent(new TrackingEvent(TrackingStatus.InTransit, "In transit", "Bloemfontein Hub", Start.AddHours(5)));
        shipment.AddEvent(new TrackingEvent(
            TrackingStatus.Exception, "Delivery attempt failed", "Midrand Depot", Start.AddHours(5), "Recipient not available"));

        var result = RoundTrip(shipment);

        Assert.Equal(
            ["Parcel packed", "In transit", "Delivery attempt failed"],
            result.Events.Select(e => e.Description));
        Assert.Equal(TrackingStatus.Exception, result.CurrentStatus);
        Assert.Equal("Recipient not available", result.CurrentEvent.ExceptionReason);
        Assert.Equal(Start.AddHours(5), result.LastUpdated);
    }

    [Fact]
    public void ToDocument_ConvertsTimestampWithOffsetToUtc()
    {
        var johannesburgTime = new DateTimeOffset(2026, 10, 8, 7, 42, 0, TimeSpan.FromHours(2));
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Midrand Depot", johannesburgTime));

        var document = ShipmentDocumentMapper.ToDocument(shipment, CreatedAt);

        var timestamp = Assert.Single(document.Events).Timestamp;
        Assert.Equal(new DateTime(2026, 10, 8, 5, 42, 0), timestamp);
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
    }

    [Fact]
    public void ToDomain_ReturnsTimestampsWithZeroOffset()
    {
        var johannesburgTime = new DateTimeOffset(2026, 10, 8, 7, 42, 0, TimeSpan.FromHours(2));
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Midrand Depot", johannesburgTime));

        var timestamp = Assert.Single(RoundTrip(shipment).Events).Timestamp;

        Assert.Equal(johannesburgTime, timestamp);
        Assert.Equal(TimeSpan.Zero, timestamp.Offset);
    }

    [Fact]
    public void ToDocument_FormatsEstimatedDeliveryAsIsoDate()
    {
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Cape Town Hub", Start));

        var document = ShipmentDocumentMapper.ToDocument(shipment, CreatedAt);

        Assert.Equal("2026-10-09", document.EstimatedDelivery);
    }

    [Fact]
    public void ToDomain_DocumentWithNoEvents_Throws()
    {
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.Packed, "Parcel packed", "Cape Town Hub", Start));
        var document = ShipmentDocumentMapper.ToDocument(shipment, CreatedAt);
        document.Events.Clear();

        Assert.Throws<InvalidOperationException>(() => ShipmentDocumentMapper.ToDomain(document));
    }

    [Fact]
    public void ShipmentDocument_SerializesToContractShape()
    {
        MongoConventions.Register();
        var shipment = CreateShipment(new TrackingEvent(TrackingStatus.OutForDelivery, "Out for delivery", "Midrand Depot", Start));

        var bson = ShipmentDocumentMapper.ToDocument(shipment, CreatedAt).ToBsonDocument();

        Assert.Equal(
            ["_id", "waybill", "receiver", "estimatedDelivery", "events", "cancelledAt", "createdAt"],
            bson.Names);
        Assert.Equal(["name", "city", "phone", "streetAddress"], bson["receiver"].AsBsonDocument.Names);
        Assert.Equal(BsonType.String, bson["estimatedDelivery"].BsonType);
        Assert.Equal(BsonType.DateTime, bson["createdAt"].BsonType);

        var trackingEvent = bson["events"].AsBsonArray.Single().AsBsonDocument;
        Assert.Equal(["status", "description", "location", "timestamp", "exceptionReason"], trackingEvent.Names);
        Assert.Equal("OutForDelivery", trackingEvent["status"].AsString);
        Assert.Equal(BsonType.DateTime, trackingEvent["timestamp"].BsonType);
    }
}
