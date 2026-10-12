using System.Net;
using System.Text.Json;
using DeliveryTracking.Api.Data;
using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Tests.Fakes;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DeliveryTracking.Api.Tests.Integration;

// These tests read the raw JSON rather than deserialising into the C# records,
// so a wrong serializer setting in Program.cs shows up as a failure.
public class TrackingEndpointTests : IClassFixture<TrackingApiFactory>
{
    private const string ProblemJson = "application/problem+json";

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

    [Fact]
    public async Task GetTracking_BadlyFormattedWaybill_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/api/tracking/hello");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", json.GetProperty("type").GetString());
        Assert.Equal("Invalid waybill format", json.GetProperty("title").GetString());
        Assert.Equal(400, json.GetProperty("status").GetInt32());
        Assert.Equal("A waybill is WB followed by 8 digits, for example WB10000004.", json.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetTracking_RepositoryThrows_Returns500ProblemDetailsWithoutInternals()
    {
        using var failingFactory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IShipmentRepository>();
            services.AddSingleton<IShipmentRepository, ThrowingShipmentRepository>();
        }));
        var client = failingFactory.CreateClient();

        var response = await client.GetAsync("/api/tracking/WB10000004");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.DoesNotContain(ThrowingShipmentRepository.SecretMessage, body);
        Assert.DoesNotContain(nameof(ThrowingShipmentRepository), body);
    }

    [Fact]
    public async Task GetTracking_UnknownRoute_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
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
    public async Task GetHealth_AppRunning_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://evil.example", false)]
    public async Task Cors_Preflight_AllowsOnlyConfiguredOrigin(string origin, bool expectedAllowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tracking/WB10000004");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        var allowed = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
            && values.Single() == origin;
        Assert.Equal(expectedAllowed, allowed);
    }

    [Fact]
    public async Task GetOpenApiDocument_AppRunning_DescribesTrackingEndpoint()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(json.GetProperty("paths").TryGetProperty("/api/tracking/{waybill}", out _));
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
