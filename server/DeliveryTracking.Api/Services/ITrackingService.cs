using DeliveryTracking.Api.Contracts;

namespace DeliveryTracking.Api.Services;

public interface ITrackingService
{
    /// <returns>Null when no active shipment has this waybill.</returns>
    Task<TrackingResponse?> GetTrackingAsync(string waybill, CancellationToken ct);

    /// <summary>Every active waybill, sorted.</summary>
    Task<IReadOnlyList<string>> GetDemoWaybillsAsync(CancellationToken ct);
}
