namespace DeliveryTracking.Api.Domain;

/// <remarks>
/// Phone and StreetAddress are kept for the courier's own use. The API never
/// returns them.
/// </remarks>
public sealed class Receiver
{
    public Receiver(string name, string city, string phone, string streetAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(streetAddress);

        Name = name;
        City = city;
        Phone = phone;
        StreetAddress = streetAddress;
    }

    public string Name { get; }
    public string City { get; }
    public string Phone { get; }
    public string StreetAddress { get; }
}
