using DeliveryTracking.Api.Data;

namespace DeliveryTracking.Api.Tests.Fakes;

// The integration tests rely on this fake behaving like MongoShipmentRepository,
// so the behaviour they depend on is pinned down here.
public class InMemoryShipmentRepositoryTests
{
    [Fact]
    public async Task GetByWaybillAsync_LowercaseWithWhitespace_FindsShipment()
    {
        var repository = new InMemoryShipmentRepository(SeedData.CreateShipments());

        var result = await repository.GetByWaybillAsync("  wb10000004 ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WB10000004", result.Waybill);
    }

    [Fact]
    public void Constructor_DuplicateWaybill_Throws()
    {
        var shipments = SeedData.CreateShipments().Concat(SeedData.CreateShipments());

        Assert.Throws<ArgumentException>(() => new InMemoryShipmentRepository(shipments));
    }
}
