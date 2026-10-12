using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DeliveryTracking.Api.Data.Mongo;

/// <remarks>
/// Makes /health report Unhealthy when MongoDB stops answering, so Render
/// shows the service as down instead of it answering every lookup with a 500.
/// </remarks>
public sealed class MongoHealthCheck : IHealthCheck
{
    private static readonly BsonDocumentCommand<BsonDocument> Ping = new(new BsonDocument("ping", 1));

    private readonly IMongoClient _client;
    private readonly string _databaseName;

    public MongoHealthCheck(IMongoClient client, IOptions<MongoSettings> settings)
    {
        _client = client;
        _databaseName = settings.Value.DatabaseName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetDatabase(_databaseName).RunCommandAsync(Ping, cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        // Any failure means the API can't serve lookups, whatever the cause:
        // no route to Atlas, bad credentials, or the timeout cancelling the ping.
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB did not answer a ping.", ex);
        }
    }
}
