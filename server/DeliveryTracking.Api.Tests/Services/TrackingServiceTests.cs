using System.Globalization;
using System.Text.Json;
using DeliveryTracking.Api.Contracts;
using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;
using DeliveryTracking.Api.Services;
using DeliveryTracking.Api.Tests.Fakes;

namespace DeliveryTracking.Api.Tests.Services;

public class TrackingServiceTests
{
    private const string Waybill = "WB10000099";
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    // No "+" in the phone number: the default JSON encoder escapes "+" as
    // \u002B, which would let the privacy test pass even if the phone leaked.
    private const string Phone = "082 555 0199";
    private const string StreetAddress = "1 Example Street, Parktown";

    private static Shipment CreateShipment(TrackingEvent initialEvent, params TrackingEvent[] laterEvents)
    {
        var shipment = new Shipment(
            Waybill,
            new Receiver("T. Mokoena", "Johannesburg", Phone, StreetAddress),
            new DateOnly(2026, 10, 9),
            initialEvent);

        foreach (var trackingEvent in laterEvents)
        {
            shipment.AddEvent(trackingEvent);
        }

        return shipment;
    }

    private static TrackingService CreateService(params IEnumerable<Shipment> shipments) =>
        new(new InMemoryShipmentRepository(shipments));

    private static TrackingEvent Packed(DateTimeOffset at) =>
        new(TrackingStatus.Packed, "Parcel packed", "Cape Town Warehouse", at);

    [Fact]
    public async Task GetTrackingAsync_UnknownWaybill_ReturnsNull()
    {
        var service = CreateService(CreateShipment(Packed(Start)));

        var result = await service.GetTrackingAsync("WB99999999", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrackingAsync_KnownWaybill_ResponseHasNoPhoneOrAddress()
    {
        var service = CreateService(CreateShipment(Packed(Start)));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain(Phone, json);
        Assert.DoesNotContain(StreetAddress, json);

        using var document = JsonDocument.Parse(json);
        var receiverFields = document.RootElement.GetProperty("Receiver").EnumerateObject().Select(p => p.Name);
        Assert.Equal(["Name", "City"], receiverFields);
    }

    [Fact]
    public async Task GetTrackingAsync_EventsRecordedOutOfOrder_ReturnsNewestFirst()
    {
        var service = CreateService(CreateShipment(
            Packed(Start),
            new TrackingEvent(TrackingStatus.OutForDelivery, "Out for delivery", "Midrand Depot", Start.AddHours(30)),
            new TrackingEvent(TrackingStatus.InTransit, "In transit", "Johannesburg Hub", Start.AddHours(10))));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            [
                new TrackingEventDto(TrackingStatus.OutForDelivery, "Out for delivery", "Midrand Depot", Start.AddHours(30)),
                new TrackingEventDto(TrackingStatus.InTransit, "In transit", "Johannesburg Hub", Start.AddHours(10)),
                new TrackingEventDto(TrackingStatus.Packed, "Parcel packed", "Cape Town Warehouse", Start),
            ],
            result.Events);
        Assert.Equal(TrackingStatus.OutForDelivery, result.Status);
    }

    [Fact]
    public async Task GetTrackingAsync_EventsWithMixedOffsets_SortsByInstantNotWallClock()
    {
        // 08:00+02:00 is 06:00 UTC, so it is older than 06:30+00:00 even though
        // its clock time is later. 08:30+02:00 is the same instant as 06:30 UTC
        // and was recorded later, so it wins the tie.
        var service = CreateService(CreateShipment(
            Packed(new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.FromHours(2))),
            new TrackingEvent(
                TrackingStatus.InTransit, "Departed hub", "Cape Town Hub", new DateTimeOffset(2026, 10, 8, 6, 30, 0, TimeSpan.Zero)),
            new TrackingEvent(
                TrackingStatus.InTransit, "Arrived at regional hub", "Johannesburg Hub",
                new DateTimeOffset(2026, 10, 8, 8, 30, 0, TimeSpan.FromHours(2)))));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            ["Arrived at regional hub", "Departed hub", "Parcel packed"],
            result.Events.Select(e => e.Description));
        Assert.Equal(TrackingStatus.InTransit, result.Status);
        Assert.Equal(result.LastUpdated, result.Events[0].Timestamp);
    }

    [Fact]
    public async Task GetTrackingAsync_EventsWithSameTimestamp_LaterRecordedComesFirst()
    {
        var service = CreateService(CreateShipment(
            Packed(Start),
            new TrackingEvent(TrackingStatus.Processing, "Received at hub", "Cape Town Hub", Start.AddHours(5)),
            new TrackingEvent(TrackingStatus.InTransit, "Departed hub", "Cape Town Hub", Start.AddHours(5))));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(
            [TrackingStatus.InTransit, TrackingStatus.Processing, TrackingStatus.Packed],
            result.Events.Select(e => e.Status));
        Assert.Equal(result.Status, result.Events[0].Status);
    }

    [Fact]
    public async Task GetTrackingAsync_ExceptionShipment_SetsExceptionReason()
    {
        var service = CreateService(CreateShipment(
            Packed(Start),
            new TrackingEvent(
                TrackingStatus.Exception, "Delivery attempt failed", "Midrand", Start.AddHours(30), "Recipient not available")));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(TrackingStatus.Exception, result.Status);
        Assert.True(result.IsException);
        Assert.Equal("Recipient not available", result.ExceptionReason);
        Assert.Equal(4, result.StepIndex);
    }

    [Fact]
    public async Task GetTrackingAsync_NonExceptionShipment_ExceptionReasonIsNull()
    {
        var service = CreateService(CreateShipment(
            Packed(Start),
            new TrackingEvent(
                TrackingStatus.Exception, "Delivery attempt failed", "Midrand", Start.AddHours(30), "Recipient not available"),
            new TrackingEvent(TrackingStatus.OutForDelivery, "Out for delivery again", "Midrand Depot", Start.AddHours(48))));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsException);
        Assert.Null(result.ExceptionReason);
        Assert.Equal(3, result.StepIndex);
    }

    [Fact]
    public async Task GetTrackingAsync_TimestampsWithOffset_ReturnedAsUtc()
    {
        var johannesburgTime = new DateTimeOffset(2026, 10, 8, 7, 42, 0, TimeSpan.FromHours(2));
        var service = CreateService(CreateShipment(Packed(johannesburgTime)));

        var result = await service.GetTrackingAsync(Waybill, CancellationToken.None);

        Assert.NotNull(result);
        var expected = new DateTimeOffset(2026, 10, 8, 5, 42, 0, TimeSpan.Zero);
        Assert.Equal(expected, result.LastUpdated);
        Assert.Equal(TimeSpan.Zero, result.LastUpdated.Offset);
        Assert.Equal(TimeSpan.Zero, Assert.Single(result.Events).Timestamp.Offset);
    }

    [Fact]
    public async Task GetTrackingAsync_SeedShipment_MatchesContractExample()
    {
        var service = CreateService(SeedData.CreateShipments());

        var result = await service.GetTrackingAsync("WB10000004", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WB10000004", result.Waybill);
        Assert.Equal(TrackingStatus.OutForDelivery, result.Status);
        Assert.Equal(3, result.StepIndex);
        Assert.False(result.IsException);
        Assert.Null(result.ExceptionReason);
        Assert.Equal("T. Mokoena", result.Receiver.Name);
        Assert.Equal("Johannesburg", result.Receiver.City);
        Assert.Equal(new DateOnly(2026, 10, 9), result.EstimatedDelivery);
        Assert.Equal("2026-10-08T05:42:00+00:00", result.LastUpdated.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture));
        Assert.Equal(5, result.Events.Count);

        var newest = result.Events[0];
        Assert.Equal(TrackingStatus.OutForDelivery, newest.Status);
        Assert.Equal("Parcel out for delivery", newest.Description);
        Assert.Equal("Midrand Depot", newest.Location);
        Assert.Equal(result.LastUpdated, newest.Timestamp);
    }

    [Fact]
    public async Task GetDemoWaybillsAsync_ShipmentsInReverseOrder_ReturnsSortedWaybills()
    {
        var service = CreateService(SeedData.CreateShipments().Reverse());

        var result = await service.GetDemoWaybillsAsync(CancellationToken.None);

        Assert.Equal(
            ["WB10000001", "WB10000002", "WB10000003", "WB10000004", "WB10000005", "WB10000006"],
            result);
    }
}
