using System.Text.Json.Serialization;
using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Services;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).WithMethods("GET")));

builder.Services.AddMongo();
builder.Services.AddScoped<ITrackingService, TrackingService>();

var app = builder.Build();

// Seeds and exits without starting the web server.
if (args.Contains("--seed"))
{
    await app.Services.GetRequiredService<ShipmentSeeder>().SeedAsync(CancellationToken.None);
    return;
}

app.UseExceptionHandler();
// Gives bodiless errors, such as a 404 for an unknown route, a ProblemDetails body.
app.UseStatusCodePages();
app.UseCors(ClientCorsPolicy);

// Enabled in every environment so a reviewer can try the deployed API.
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Delivery Tracking API"));

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
