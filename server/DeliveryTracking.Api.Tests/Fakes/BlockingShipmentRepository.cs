using System.Diagnostics;
using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Domain;

namespace DeliveryTracking.Api.Tests.Fakes;

/// <remarks>
/// Every call waits until its token is cancelled, so a test can prove the
/// request's token reaches the repository. CancellationToken.None would
/// leave the call waiting forever.
/// </remarks>
public sealed class BlockingShipmentRepository : IShipmentRepository
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;
    public Task Cancelled => _cancelled.Task;

    public Task<Shipment?> GetByWaybillAsync(string waybill, CancellationToken ct) =>
        WaitForCancellationAsync<Shipment?>(ct);

    public Task<IReadOnlyList<string>> GetAllWaybillsAsync(CancellationToken ct) =>
        WaitForCancellationAsync<IReadOnlyList<string>>(ct);

    private async Task<T> WaitForCancellationAsync<T>(CancellationToken ct)
    {
        _started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _cancelled.TrySetResult();
            throw;
        }

        throw new UnreachableException();
    }
}
