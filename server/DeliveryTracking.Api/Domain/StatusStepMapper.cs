namespace DeliveryTracking.Api.Domain;

/// <summary>
/// The only place that maps a status to a stepper step. The client uses the
/// index it is given and never calculates one.
/// </summary>
public static class StatusStepMapper
{
    /// <summary>
    /// 0 to 4. Exception shares step 4 with Delivered, because the parcel's
    /// journey stops there either way.
    /// </summary>
    public static int GetStepIndex(TrackingStatus status) => status switch
    {
        TrackingStatus.Packed => 0,
        TrackingStatus.Processing => 1,
        TrackingStatus.InTransit => 2,
        TrackingStatus.OutForDelivery => 3,
        TrackingStatus.Delivered => 4,
        TrackingStatus.Exception => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown tracking status.")
    };

    public static bool IsException(TrackingStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown tracking status.");
        }

        return status == TrackingStatus.Exception;
    }
}
