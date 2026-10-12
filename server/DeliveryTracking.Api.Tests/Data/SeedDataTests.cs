using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Data;

public class SeedDataTests
{
    [Fact]
    public void CreateShipments_HasOneShipmentPerStatus()
    {
        var shipments = SeedData.CreateShipments();

        Assert.Equal(
            ["WB10000001", "WB10000002", "WB10000003", "WB10000004", "WB10000005", "WB10000006"],
            shipments.Select(s => s.Waybill));
        Assert.Equal(
            [
                TrackingStatus.Packed,
                TrackingStatus.Processing,
                TrackingStatus.InTransit,
                TrackingStatus.OutForDelivery,
                TrackingStatus.Delivered,
                TrackingStatus.Exception,
            ],
            shipments.Select(s => s.CurrentStatus));
        Assert.Equal("Recipient not available", shipments[5].CurrentEvent.ExceptionReason);
    }

    [Fact]
    public void CreateShipments_ReturnsNewInstancesEachCall()
    {
        var first = SeedData.CreateShipments();
        var second = SeedData.CreateShipments();

        Assert.NotSame(first[0], second[0]);
    }
}
