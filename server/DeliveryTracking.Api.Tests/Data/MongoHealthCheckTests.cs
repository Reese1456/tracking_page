using DeliveryTracking.Api.Data.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DeliveryTracking.Api.Tests.Data;

public class MongoHealthCheckTests
{
    // Nothing listens on port 1, and the short timeouts keep the test fast.
    private const string UnreachableServer = "mongodb://localhost:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";

    [Fact]
    public async Task CheckHealthAsync_ServerNotAnswering_ReturnsUnhealthy()
    {
        var settings = Options.Create(new MongoSettings { ConnectionString = UnreachableServer, DatabaseName = "deliverytracking" });
        var check = new MongoHealthCheck(new MongoClient(UnreachableServer), settings);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("MongoDB did not answer a ping.", result.Description);
    }

    [Fact]
    public void AddMongo_RegistersHealthCheckWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddMongo();

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var registration = Assert.Single(options.Registrations);
        Assert.Equal(MongoServiceCollectionExtensions.HealthCheckName, registration.Name);
        Assert.Equal(TimeSpan.FromSeconds(3), registration.Timeout);
    }
}
