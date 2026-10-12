using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DeliveryTracking.Api.Tests.Fakes;

/// <remarks>Stands in for the MongoDB ping, which would need a real database.</remarks>
public sealed class StubHealthCheck : IHealthCheck
{
    private readonly HealthCheckResult _result;

    public StubHealthCheck(HealthCheckResult result)
    {
        _result = result;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}
