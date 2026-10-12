using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Fakes;

/// <remarks>Stands in for a database failure, to test the 500 response.</remarks>
public sealed class ThrowingShipmentRepository : IShipmentRepository
{
    public const string SecretMessage = "Connection to cluster0-shard-00 failed";

    public Task<Shipment?> GetByWaybillAsync(string waybill, CancellationToken ct) =>
        throw new InvalidOperationException(SecretMessage);

    public Task<IReadOnlyList<string>> GetAllWaybillsAsync(CancellationToken ct) =>
        throw new InvalidOperationException(SecretMessage);
}
