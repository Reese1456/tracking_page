namespace DeliveryTracking.Api.Contracts;

/// <remarks>
/// Name and city only. The receiver's phone and street address have no field
/// here, so they can't reach a public tracking page.
/// </remarks>
public sealed record ReceiverDto(string Name, string City);
