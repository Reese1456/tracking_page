using System.Globalization;
using DeliveryTracking.Api.Domain;
using MongoDB.Bson;

namespace DeliveryTracking.Api.Data.Mongo;

public static class ShipmentDocumentMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <remarks>
    /// Builds the Shipment through its constructor and AddEvent, so a stored
    /// document that breaks a domain rule fails here instead of reaching the API.
    /// </remarks>
    public static Shipment ToDomain(ShipmentDocument document)
    {
        if (document.Events.Count == 0)
        {
            throw new InvalidOperationException(
                $"Shipment {document.Waybill} has no events, so its status can't be derived.");
        }

        var receiver = new Receiver(
            document.Receiver.Name,
            document.Receiver.City,
            document.Receiver.Phone,
            document.Receiver.StreetAddress);

        var estimatedDelivery = DateOnly.ParseExact(document.EstimatedDelivery, DateFormat, CultureInfo.InvariantCulture);

        // Stored order is kept because it decides which event wins a timestamp tie.
        var shipment = new Shipment(document.Waybill, receiver, estimatedDelivery, ToDomain(document.Events[0]));
        foreach (var eventDocument in document.Events.Skip(1))
        {
            shipment.AddEvent(ToDomain(eventDocument));
        }

        return shipment;
    }

    /// <remarks>
    /// Only used to create seed documents. Id and createdAt are new values,
    /// so this is not an update path for shipments already stored.
    /// </remarks>
    public static ShipmentDocument ToDocument(Shipment shipment, DateTime createdAtUtc)
    {
        return new ShipmentDocument
        {
            Id = ObjectId.GenerateNewId(),
            Waybill = shipment.Waybill,
            Receiver = new ReceiverDocument
            {
                Name = shipment.Receiver.Name,
                City = shipment.Receiver.City,
                Phone = shipment.Receiver.Phone,
                StreetAddress = shipment.Receiver.StreetAddress,
            },
            EstimatedDelivery = shipment.EstimatedDelivery.ToString(DateFormat, CultureInfo.InvariantCulture),
            Events = shipment.Events.Select(ToDocument).ToList(),
            CancelledAt = null,
            CreatedAt = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc),
        };
    }

    private static TrackingEvent ToDomain(TrackingEventDocument document)
    {
        // BSON Dates have no offset; the driver reads them back as UTC, but
        // SpecifyKind makes that explicit so the offset is always +00:00.
        var utc = DateTime.SpecifyKind(document.Timestamp, DateTimeKind.Utc);

        return new TrackingEvent(
            document.Status,
            document.Description,
            document.Location,
            new DateTimeOffset(utc),
            document.ExceptionReason);
    }

    private static TrackingEventDocument ToDocument(TrackingEvent trackingEvent)
    {
        return new TrackingEventDocument
        {
            Status = trackingEvent.Status,
            Description = trackingEvent.Description,
            Location = trackingEvent.Location,
            Timestamp = trackingEvent.Timestamp.UtcDateTime,
            ExceptionReason = trackingEvent.ExceptionReason,
        };
    }
}
