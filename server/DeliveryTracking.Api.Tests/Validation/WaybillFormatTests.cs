using DeliveryTracking.Api.Validation;

namespace DeliveryTracking.Api.Tests.Validation;

public class WaybillFormatTests
{
    [Theory]
    [InlineData("WB10000004")]
    [InlineData("wb10000004")]
    [InlineData("  WB10000004 ")]
    public void IsValid_AcceptedInput_ReturnsTrue(string input)
    {
        var result = WaybillFormat.IsValid(input);

        Assert.True(result);
    }

    [Theory]
    [InlineData("WB1000000")]
    [InlineData("WB100000004")]
    [InlineData("XB10000004")]
    [InlineData("10000004")]
    [InlineData("WB 10000004")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("WB1000000٤")]
    public void IsValid_InvalidInput_ReturnsFalse(string input)
    {
        var result = WaybillFormat.IsValid(input);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_Null_ReturnsFalse()
    {
        var result = WaybillFormat.IsValid(null);

        Assert.False(result);
    }

    [Fact]
    public void Normalize_LowercaseWithWhitespace_ReturnsTrimmedUppercase()
    {
        var result = WaybillFormat.Normalize("  wb10000004\t");

        Assert.Equal("WB10000004", result);
    }
}
