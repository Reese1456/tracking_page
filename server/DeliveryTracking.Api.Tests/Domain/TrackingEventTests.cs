using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Domain;

public class TrackingEventTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 10, 8, 5, 42, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ExceptionWithoutReason_Throws(string? reason)
    {
        Assert.Throws<ArgumentException>(() =>
            new TrackingEvent(TrackingStatus.Exception, "Delivery failed", "Midrand Depot", Timestamp, reason));
    }

    [Fact]
    public void Constructor_ReasonOnNonExceptionEvent_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TrackingEvent(TrackingStatus.InTransit, "In transit", "Midrand Depot", Timestamp, "Recipient not available"));
    }

    [Fact]
    public void Constructor_ExceptionWithReason_KeepsReason()
    {
        var trackingEvent = new TrackingEvent(
            TrackingStatus.Exception, "Delivery failed", "Midrand Depot", Timestamp, "Recipient not available");

        Assert.Equal("Recipient not available", trackingEvent.ExceptionReason);
    }
}
