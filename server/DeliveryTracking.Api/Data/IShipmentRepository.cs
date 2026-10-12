using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Data;

/// <remarks>Cancelled shipments are never returned.</remarks>
public interface IShipmentRepository
{
    /// <summary>The waybill is trimmed and uppercased before the lookup.</summary>
    Task<Shipment?> GetByWaybillAsync(string waybill, CancellationToken ct);

    /// <summary>Sorted.</summary>
    Task<IReadOnlyList<string>> GetAllWaybillsAsync(CancellationToken ct);
}
