namespace DeliveryTracking.Api.Domain;

public enum TrackingStatus
{
    Packed,
    Processing,
    InTransit,
    OutForDelivery,
    Delivered,
    Exception
}
