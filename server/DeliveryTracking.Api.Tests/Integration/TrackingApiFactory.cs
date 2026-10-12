using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace DeliveryTracking.Api.Tests.Integration;

/// <remarks>
/// Runs the real app in memory with the seed shipments and no database.
/// Nothing resolves IMongoClient, so no connection is ever attempted; even
/// the health check's ping is replaced.
/// </remarks>
public sealed class TrackingApiFactory : WebApplicationFactory<Program>
{
    public const string DummyConnectionString = "mongodb://localhost";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Added last, so it overrides a real connection string in local
        // user-secrets. mongodb://localhost because a mongodb+srv string
        // triggers a DNS lookup as soon as a client is created.
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = DummyConnectionString,
            ["Mongo:DatabaseName"] = "deliverytracking",
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IShipmentRepository>();
            services.AddSingleton<IShipmentRepository>(new InMemoryShipmentRepository(SeedData.CreateShipments()));

            var indexInitializer = services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(ShipmentIndexInitializer));
            services.Remove(indexInitializer);

            ReplaceMongoHealthCheck(services, HealthCheckResult.Healthy());
        });
    }

    /// <remarks>
    /// Runs after the app's own registration, so it can swap the MongoDB ping
    /// for a fixed answer. Single() fails every test if the app stops
    /// registering the check, instead of the swap silently doing nothing.
    /// </remarks>
    public static void ReplaceMongoHealthCheck(IServiceCollection services, HealthCheckResult result) =>
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            var mongoCheck = options.Registrations.Single(r => r.Name == MongoServiceCollectionExtensions.HealthCheckName);
            options.Registrations.Remove(mongoCheck);
            options.Registrations.Add(new HealthCheckRegistration(
                mongoCheck.Name, new StubHealthCheck(result), failureStatus: null, tags: null));
        });
}
