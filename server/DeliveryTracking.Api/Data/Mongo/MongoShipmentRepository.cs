using DeliveryTracking.Api.Domain;
using MongoDB.Driver;

namespace DeliveryTracking.Api.Data.Mongo;

public sealed class MongoShipmentRepository : IShipmentRepository
{
    private static readonly FilterDefinitionBuilder<ShipmentDocument> Filter = Builders<ShipmentDocument>.Filter;
    private static readonly FilterDefinition<ShipmentDocument> NotCancelled = Filter.Eq(d => d.CancelledAt, null);

    private readonly IMongoCollection<ShipmentDocument> _shipments;

    public MongoShipmentRepository(IMongoCollection<ShipmentDocument> shipments)
    {
        _shipments = shipments;
    }

    public async Task<Shipment?> GetByWaybillAsync(string waybill, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(waybill);

        // Waybills are stored uppercase, so this is an exact match on the unique index.
        var filter = Filter.Eq(d => d.Waybill, waybill.Trim().ToUpperInvariant()) & NotCancelled;

        var document = await _shipments.Find(filter).FirstOrDefaultAsync(ct);
        return document is null ? null : ShipmentDocumentMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<string>> GetAllWaybillsAsync(CancellationToken ct)
    {
        return await _shipments
            .Find(NotCancelled)
            .SortBy(d => d.Waybill)
            .Project(d => d.Waybill)
            .ToListAsync(ct);
    }
}
