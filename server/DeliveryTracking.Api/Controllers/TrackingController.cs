using DeliveryTracking.Api.Contracts;
using DeliveryTracking.Api.Services;
using DeliveryTracking.Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryTracking.Api.Controllers;

[ApiController]
[Route("api/tracking")]
public sealed class TrackingController : ControllerBase
{
    private readonly ITrackingService _tracking;

    public TrackingController(ITrackingService tracking)
    {
        _tracking = tracking;
    }

    [HttpGet("{waybill}")]
    [ProducesResponseType<TrackingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    // Nullable because model binding turns a whitespace-only value into null.
    // As a non-nullable string, [ApiController] would reject it with a
    // generic validation error before the format check could run.
    public async Task<ActionResult<TrackingResponse>> GetTracking(string? waybill, CancellationToken ct)
    {
        if (!WaybillFormat.IsValid(waybill))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid waybill format",
                detail: "A waybill is WB followed by 8 digits, for example WB10000004.");
        }

        var normalized = WaybillFormat.Normalize(waybill);
        var tracking = await _tracking.GetTrackingAsync(normalized, ct);

        if (tracking is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"No shipment was found for waybill {normalized}.");
        }

        return Ok(tracking);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDemoWaybills(CancellationToken ct) =>
        Ok(await _tracking.GetDemoWaybillsAsync(ct));
}
