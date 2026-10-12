using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Contracts;

/// <summary>The body of GET /api/tracking/{waybill}, as defined in docs/api-contract.md.</summary>
/// <param name="Status">Derived from the newest event.</param>
/// <param name="StepIndex">0 to 4, from StatusStepMapper. The client uses it as given.</param>
/// <param name="ExceptionReason">Set only when IsException is true.</param>
/// <param name="LastUpdated">The timestamp of the newest event.</param>
/// <param name="Events">Newest first.</param>
public sealed record TrackingResponse(
    string Waybill,
    TrackingStatus Status,
    int StepIndex,
    bool IsException,
    string? ExceptionReason,
    ReceiverDto Receiver,
    DateOnly EstimatedDelivery,
    DateTimeOffset LastUpdated,
    IReadOnlyList<TrackingEventDto> Events);
