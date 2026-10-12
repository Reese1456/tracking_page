using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Fakes;

/// <remarks>
/// Behaves like MongoShipmentRepository without a database: lookups are
/// trimmed and uppercased, and a duplicate waybill throws, like the unique index.
/// </remarks>
public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly Dictionary<string, Shipment> _shipments = [];

    public InMemoryShipmentRepository(IEnumerable<Shipment> shipments)
    {
        foreach (var shipment in shipments)
        {
            _shipments.Add(shipment.Waybill.ToUpperInvariant(), shipment);
        }
    }

    public Task<Shipment?> GetByWaybillAsync(string waybill, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(waybill);

        _shipments.TryGetValue(waybill.Trim().ToUpperInvariant(), out var shipment);
        return Task.FromResult(shipment);
    }

    public Task<IReadOnlyList<string>> GetAllWaybillsAsync(CancellationToken ct)
    {
        IReadOnlyList<string> waybills = _shipments.Keys.Order(StringComparer.Ordinal).ToList();
        return Task.FromResult(waybills);
    }
}
