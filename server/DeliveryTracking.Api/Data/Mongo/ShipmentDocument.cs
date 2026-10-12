using MongoDB.Bson;

namespace DeliveryTracking.Api.Data.Mongo;

public sealed class ShipmentDocument
{
    public ObjectId Id { get; set; }
    public string Waybill { get; set; } = "";
    public ReceiverDocument Receiver { get; set; } = new();

    /// <summary>A yyyy-MM-dd string, because it is a date with no time of day.</summary>
    public string EstimatedDelivery { get; set; } = "";

    /// <summary>In the order the events were recorded, which breaks timestamp ties.</summary>
    public List<TrackingEventDocument> Events { get; set; } = [];

    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
