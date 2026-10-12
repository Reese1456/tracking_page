using MongoDB.Driver;

namespace DeliveryTracking.Api.Data.Mongo;

/// <remarks>
/// Upserts by waybill. Demo content is reset to SeedData on every run, but
/// _id and createdAt are only written when a shipment is first inserted.
/// </remarks>
public sealed class ShipmentSeeder
{
    private readonly IMongoCollection<ShipmentDocument> _shipments;
    private readonly ILogger<ShipmentSeeder> _logger;

    public ShipmentSeeder(IMongoCollection<ShipmentDocument> shipments, ILogger<ShipmentSeeder> logger)
    {
        _shipments = shipments;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        // The index must exist first, or a duplicate waybill could slip in.
        await ShipmentIndexInitializer.EnsureIndexesAsync(_shipments, ct);

        var createdAt = DateTime.UtcNow;
        var upserts = SeedData.CreateShipments()
            .Select(shipment => ShipmentDocumentMapper.ToDocument(shipment, createdAt))
            .Select(ToUpsert)
            .ToList();

        var result = await _shipments.BulkWriteAsync(upserts, cancellationToken: ct);

        _logger.LogInformation(
            "Seed complete: {Inserted} inserted, {Modified} reset, {Unchanged} unchanged.",
            result.Upserts.Count,
            result.ModifiedCount,
            result.MatchedCount - result.ModifiedCount);
    }

    private static UpdateOneModel<ShipmentDocument> ToUpsert(ShipmentDocument document)
    {
        var filter = Builders<ShipmentDocument>.Filter.Eq(d => d.Waybill, document.Waybill);

        // Setting a field to the value it already has doesn't count as a
        // change, so a second run reports every shipment as unchanged.
        var update = Builders<ShipmentDocument>.Update
            .Set(d => d.Receiver, document.Receiver)
            .Set(d => d.EstimatedDelivery, document.EstimatedDelivery)
            .Set(d => d.Events, document.Events)
            .Set(d => d.CancelledAt, null)
            .SetOnInsert(d => d.Id, document.Id)
            .SetOnInsert(d => d.CreatedAt, document.CreatedAt);

        return new UpdateOneModel<ShipmentDocument>(filter, update) { IsUpsert = true };
    }
}
