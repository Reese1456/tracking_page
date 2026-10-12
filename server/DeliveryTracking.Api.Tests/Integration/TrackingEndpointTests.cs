using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DeliveryTracking.Api.Tests.Integration;

// These tests read the raw JSON rather than deserialising into the C# records,
// so a wrong serializer setting in Program.cs shows up as a failure.
public class TrackingEndpointTests : IClassFixture<TrackingApiFactory>
{
    private const string ProblemJson = "application/problem+json";
    private const string ClientOrigin = "http://localhost:5173";

    private readonly TrackingApiFactory _factory;
    private readonly HttpClient _client;

    public TrackingEndpointTests(TrackingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static string[] PropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();

    // The parent factory disposes derived factories, so the caller needn't.
    private HttpClient CreateClientWith(IShipmentRepository repository) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IShipmentRepository>();
            services.AddSingleton(repository);
        })).CreateClient();

    private static HttpRequestMessage Preflight(string origin, string method = "GET")
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tracking/WB10000004");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        return request;
    }

    [Fact]
    public async Task GetTracking_KnownWaybill_ReturnsContractJson()
    {
        var response = await _client.GetAsync("/api/tracking/WB10000004");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["estimatedDelivery", "events", "exceptionReason", "isException", "lastUpdated", "receiver", "status", "stepIndex", "waybill"],
            PropertyNames(json));
        Assert.Equal("WB10000004", json.GetProperty("waybill").GetString());
        Assert.Equal(JsonValueKind.String, json.GetProperty("status").ValueKind);
        Assert.Equal("OutForDelivery", json.GetProperty("status").GetString());
        Assert.Equal(3, json.GetProperty("stepIndex").GetInt32());
        Assert.False(json.GetProperty("isException").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("exceptionReason").ValueKind);
        Assert.Equal(["city", "name"], PropertyNames(json.GetProperty("receiver")));
        Assert.Equal("2026-10-09", json.GetProperty("estimatedDelivery").GetString());
        Assert.Equal("2026-10-08T05:42:00+00:00", json.GetProperty("lastUpdated").GetString());

        var events = json.GetProperty("events").EnumerateArray().ToList();
        Assert.NotEmpty(events);
        Assert.All(events, e =>
        {
            Assert.Equal(["description", "location", "status", "timestamp"], PropertyNames(e));
            Assert.Equal(JsonValueKind.String, e.GetProperty("status").ValueKind);
            Assert.EndsWith("+00:00", e.GetProperty("timestamp").GetString());
        });

        // The seed phone starts with "+", which the encoder escapes, so check
        // a fragment without it.
        Assert.DoesNotContain("555 0104", body);
        Assert.DoesNotContain("Jan Smuts", body);
    }

    [Fact]
    public async Task GetTracking_LowercaseWaybill_Returns200WithUppercaseWaybill()
    {
        var response = await _client.GetAsync("/api/tracking/wb10000004");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("WB10000004", json.GetProperty("waybill").GetString());
    }

    [Fact]
    public async Task GetTracking_ExceptionShipment_ReturnsReasonAndStep4()
    {
        var response = await _client.GetAsync("/api/tracking/WB10000006");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Exception", json.GetProperty("status").GetString());
        Assert.Equal(4, json.GetProperty("stepIndex").GetInt32());
        Assert.True(json.GetProperty("isException").GetBoolean());
        Assert.Equal("Recipient not available", json.GetProperty("exceptionReason").GetString());
    }

    [Fact]
    public async Task GetTracking_UnknownWaybill_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/tracking/wb99999999");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", json.GetProperty("type").GetString());
        Assert.Equal("Shipment not found", json.GetProperty("title").GetString());
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("No shipment was found for waybill WB99999999.", json.GetProperty("detail").GetString());
    }

    // %20 and %09 are a space and a tab: model binding turns a whitespace-only
    // value into null before the action runs.
    [Theory]
    [InlineData("hello")]
    [InlineData("%20")]
    [InlineData("%09")]
    public async Task GetTracking_BadlyFormattedWaybill_Returns400ProblemDetails(string waybill)
    {
        var response = await _client.GetAsync($"/api/tracking/{waybill}");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", json.GetProperty("type").GetString());
        Assert.Equal("Invalid waybill format", json.GetProperty("title").GetString());
        Assert.Equal(400, json.GetProperty("status").GetInt32());
        Assert.Equal("A waybill is WB followed by 8 digits, for example WB10000004.", json.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/html")]
    public async Task GetTracking_RepositoryThrows_Returns500ProblemDetailsWithoutInternals(string accept)
    {
        var client = CreateClientWith(new ThrowingShipmentRepository());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tracking/WB10000004");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["status", "title", "traceId", "type"], PropertyNames(json));
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.Equal("An error occurred while processing your request.", json.GetProperty("title").GetString());
        Assert.DoesNotContain(ThrowingShipmentRepository.SecretMessage, body);
        Assert.DoesNotContain(nameof(ThrowingShipmentRepository), body);
    }

    // A browser opening an API URL sends Accept: text/html; errors must still
    // be ProblemDetails.
    [Theory]
    [InlineData("GET", "/api/nope", "application/json", HttpStatusCode.NotFound)]
    [InlineData("GET", "/api/nope", "text/html", HttpStatusCode.NotFound)]
    [InlineData("POST", "/api/tracking/WB10000004", "application/json", HttpStatusCode.MethodNotAllowed)]
    [InlineData("POST", "/api/tracking/WB10000004", "text/html", HttpStatusCode.MethodNotAllowed)]
    public async Task UnmatchedRequest_AnyAccept_ReturnsProblemDetails(string method, string path, string accept, HttpStatusCode expectedStatus)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        var response = await _client.SendAsync(request);
        var json = await ReadJsonAsync(response);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, json.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("/api/tracking/WB10000004")]
    [InlineData("/api/tracking")]
    public async Task Endpoints_ClientDisconnects_CancelsRepositoryCall(string path)
    {
        var repository = new BlockingShipmentRepository();
        var client = CreateClientWith(repository);
        using var disconnect = new CancellationTokenSource();

        var request = client.GetAsync(path, disconnect.Token);
        await repository.Started.WaitAsync(TimeSpan.FromSeconds(5));
        disconnect.Cancel();

        await repository.Cancelled.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
    }

    [Fact]
    public async Task GetDemoWaybills_SeededShipments_ReturnsSortedWaybills()
    {
        var response = await _client.GetAsync("/api/tracking");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["WB10000001", "WB10000002", "WB10000003", "WB10000004", "WB10000005", "WB10000006"],
            json.EnumerateArray().Select(w => w.GetString()!).ToArray());
    }

    [Fact]
    public async Task GetHealth_DatabaseAnswering_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // Render reads anything but a 2xx or 3xx as a failed health check.
    [Fact]
    public async Task GetHealth_DatabaseNotAnswering_Returns503Unhealthy()
    {
        var client = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            TrackingApiFactory.ReplaceMongoHealthCheck(services, HealthCheckResult.Unhealthy("MongoDB did not answer a ping."))))
            .CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cors_PreflightFromConfiguredOrigin_AllowsGetOnly()
    {
        using var request = Preflight(ClientOrigin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ClientOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("GET", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    // A GET preflight can't tell WithMethods("GET") from AllowAnyMethod(),
    // because both echo GET; asking for DELETE can. ASP.NET Core still
    // answers with the policy's methods, and the browser blocks the DELETE
    // because it isn't in the list.
    [Fact]
    public async Task Cors_PreflightForDeleteFromConfiguredOrigin_AdvertisesGetOnly()
    {
        using var request = Preflight(ClientOrigin, "DELETE");

        var response = await _client.SendAsync(request);

        Assert.Equal("GET", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Cors_PreflightFromUnknownOrigin_GrantsNothing()
    {
        using var request = Preflight("https://evil.example");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Methods"));
    }

    // Production reads only appsettings.json, whose origin list is empty until
    // Render sets one, so even the development client is refused.
    [Fact]
    public async Task Cors_ProductionDefaults_GrantNothing()
    {
        var client = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();
        using var request = Preflight(ClientOrigin);

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Methods"));
    }

    [Fact]
    public async Task GetOpenApiDocument_AppRunning_DescribesTrackingEndpoint()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responses = json.GetProperty("paths").GetProperty("/api/tracking/{waybill}")
            .GetProperty("get").GetProperty("responses");
        Assert.Equal(["200", "400", "404"], PropertyNames(responses));

        var statusSchema = json.GetProperty("components").GetProperty("schemas").GetProperty("TrackingStatus");
        Assert.Equal(
            ["Packed", "Processing", "InTransit", "OutForDelivery", "Delivered", "Exception"],
            statusSchema.GetProperty("enum").EnumerateArray().Select(v => v.GetString()!).ToArray());
    }

    [Fact]
    public void Factory_Configuration_UsesDummyMongoSettings()
    {
        var settings = _factory.Services.GetRequiredService<IOptions<MongoSettings>>().Value;

        // Assert.True rather than Assert.Equal, so a failure can't print a
        // real connection string from user-secrets into the test output.
        Assert.True(
            settings.ConnectionString == TrackingApiFactory.DummyConnectionString,
            "The factory is not overriding Mongo:ConnectionString with the dummy value.");
    }
}
