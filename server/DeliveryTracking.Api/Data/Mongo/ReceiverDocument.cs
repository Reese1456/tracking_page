namespace DeliveryTracking.Api.Data.Mongo;

public sealed class ReceiverDocument
{
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Phone { get; set; } = "";
    public string StreetAddress { get; set; } = "";
}
