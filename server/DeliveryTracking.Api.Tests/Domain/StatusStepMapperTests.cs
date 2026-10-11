using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Domain;

public class StatusStepMapperTests
{
    [Theory]
    [InlineData(TrackingStatus.Packed, 0)]
    [InlineData(TrackingStatus.Processing, 1)]
    [InlineData(TrackingStatus.InTransit, 2)]
    [InlineData(TrackingStatus.OutForDelivery, 3)]
    [InlineData(TrackingStatus.Delivered, 4)]
    [InlineData(TrackingStatus.Exception, 4)]
    public void GetStepIndex_ReturnsStepForEachStatus(TrackingStatus status, int expected)
    {
        Assert.Equal(expected, StatusStepMapper.GetStepIndex(status));
    }

    [Theory]
    [InlineData(TrackingStatus.Packed, false)]
    [InlineData(TrackingStatus.Processing, false)]
    [InlineData(TrackingStatus.InTransit, false)]
    [InlineData(TrackingStatus.OutForDelivery, false)]
    [InlineData(TrackingStatus.Delivered, false)]
    [InlineData(TrackingStatus.Exception, true)]
    public void IsException_IsTrueOnlyForException(TrackingStatus status, bool expected)
    {
        Assert.Equal(expected, StatusStepMapper.IsException(status));
    }

    [Fact]
    public void GetStepIndex_UnknownStatus_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StatusStepMapper.GetStepIndex((TrackingStatus)99));
    }

    [Fact]
    public void IsException_UnknownStatus_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StatusStepMapper.IsException((TrackingStatus)99));
    }
}
