using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DeliveryTracking.Api.Data.Mongo;

public static class MongoServiceCollectionExtensions
{
    public const string ShipmentsCollectionName = "shipments";

    public static IServiceCollection AddMongo(this IServiceCollection services)
    {
        MongoConventions.Register();

        services.AddOptions<MongoSettings>()
            .BindConfiguration(MongoSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One client per process: it owns the connection pool and is thread-safe.
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString));

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return sp.GetRequiredService<IMongoClient>()
                .GetDatabase(settings.DatabaseName)
                .GetCollection<ShipmentDocument>(ShipmentsCollectionName);
        });

        // Singleton is safe because its only state is the thread-safe collection.
        services.AddSingleton<IShipmentRepository, MongoShipmentRepository>();
        services.AddSingleton<ShipmentSeeder>();
        services.AddHostedService<ShipmentIndexInitializer>();

        return services;
    }
}
