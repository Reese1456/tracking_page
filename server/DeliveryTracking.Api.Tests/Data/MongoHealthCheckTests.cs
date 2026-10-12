using System.Diagnostics;
using DeliveryTracking.Api.Data.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DeliveryTracking.Api.Tests.Data;

public class MongoHealthCheckTests
{
    // Nothing listens on port 1, and the short timeouts keep the test fast.
    private const string UnreachableServer = "mongodb://localhost:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";

    private static MongoHealthCheck CreateCheck(string connectionString) =>
        new(new MongoClient(connectionString),
            Options.Create(new MongoSettings { ConnectionString = connectionString, DatabaseName = "deliverytracking" }));

    [Fact]
    public async Task CheckHealthAsync_ServerNotAnswering_ReturnsUnhealthy()
    {
        var check = CreateCheck(UnreachableServer);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("MongoDB did not answer a ping.", result.Description);
    }

    // The registration's timeout works by cancelling the token, so the ping
    // must stop when it's cancelled instead of waiting out the driver.
    [Fact]
    public async Task CheckHealthAsync_Cancelled_StopsWaitingAndReturnsUnhealthy()
    {
        var check = CreateCheck("mongodb://localhost:1/?serverSelectionTimeoutMS=10000&connectTimeoutMS=500");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        var result = await check.CheckHealthAsync(new HealthCheckContext(), cancellation.Token);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"The check took {stopwatch.Elapsed} after being cancelled.");
    }

    [Fact]
    public void AddMongo_RegistersMongoHealthCheckWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = UnreachableServer,
            ["Mongo:DatabaseName"] = "deliverytracking",
        }).Build());
        services.AddMongo();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var registration = Assert.Single(options.Registrations);
        Assert.Equal(MongoServiceCollectionExtensions.HealthCheckName, registration.Name);
        Assert.Equal(TimeSpan.FromSeconds(3), registration.Timeout);
        Assert.IsType<MongoHealthCheck>(registration.Factory(provider));
    }
}
