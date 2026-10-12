using DeliveryTracking.Api.Contracts;
using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Services;

public sealed class TrackingService : ITrackingService
{
    private readonly IShipmentRepository _shipments;

    public TrackingService(IShipmentRepository shipments)
    {
        _shipments = shipments;
    }

    public async Task<TrackingResponse?> GetTrackingAsync(string waybill, CancellationToken ct)
    {
        var shipment = await _shipments.GetByWaybillAsync(waybill, ct);
        return shipment is null ? null : ToResponse(shipment);
    }

    public Task<IReadOnlyList<string>> GetDemoWaybillsAsync(CancellationToken ct) =>
        _shipments.GetAllWaybillsAsync(ct);

    private static TrackingResponse ToResponse(Shipment shipment)
    {
        var current = shipment.CurrentEvent;

        // Newest first. On a timestamp tie the later-recorded event counts as
        // newer, so events[0] is always the event that decided the status.
        var events = shipment.Events
            .Select((trackingEvent, recordedIndex) => (trackingEvent, recordedIndex))
            .OrderByDescending(e => e.trackingEvent.Timestamp)
            .ThenByDescending(e => e.recordedIndex)
            .Select(e => new TrackingEventDto(
                e.trackingEvent.Status,
                e.trackingEvent.Description,
                e.trackingEvent.Location,
                e.trackingEvent.Timestamp.ToUniversalTime()))
            .ToList();

        // ToUniversalTime because the contract promises UTC, and not every
        // repository returns timestamps with a zero offset.
        return new TrackingResponse(
            shipment.Waybill,
            current.Status,
            StatusStepMapper.GetStepIndex(current.Status),
            StatusStepMapper.IsException(current.Status),
            current.ExceptionReason,
            new ReceiverDto(shipment.Receiver.Name, shipment.Receiver.City),
            shipment.EstimatedDelivery,
            current.Timestamp.ToUniversalTime(),
            events);
    }
}
