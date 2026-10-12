using MongoDB.Driver;

namespace DeliveryTracking.Api.Data.Mongo;

/// <remarks>
/// Runs on every start. Creating an index that already exists with the same
/// keys, options and name does nothing, so this is safe to repeat.
/// </remarks>
public sealed class ShipmentIndexInitializer : IHostedService
{
    public const string WaybillIndexName = "waybill_unique";

    private readonly IMongoCollection<ShipmentDocument> _shipments;
    private readonly ILogger<ShipmentIndexInitializer> _logger;

    public ShipmentIndexInitializer(IMongoCollection<ShipmentDocument> shipments, ILogger<ShipmentIndexInitializer> logger)
    {
        _shipments = shipments;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(_shipments, cancellationToken);
        _logger.LogInformation("Unique index {IndexName} on shipments is in place.", WaybillIndexName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Static so the seeder can create the index without starting the host.
    public static Task EnsureIndexesAsync(IMongoCollection<ShipmentDocument> shipments, CancellationToken ct)
    {
        var index = new CreateIndexModel<ShipmentDocument>(
            Builders<ShipmentDocument>.IndexKeys.Ascending(d => d.Waybill),
            new CreateIndexOptions { Unique = true, Name = WaybillIndexName });

        return shipments.Indexes.CreateOneAsync(index, cancellationToken: ct);
    }
}
