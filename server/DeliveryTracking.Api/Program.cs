using DeliveryTracking.Api.Data.Mongo;
using DeliveryTracking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMongo();
builder.Services.AddScoped<ITrackingService, TrackingService>();

var app = builder.Build();

// Seeds and exits without starting the web server.
if (args.Contains("--seed"))
{
    await app.Services.GetRequiredService<ShipmentSeeder>().SeedAsync(CancellationToken.None);
    return;
}

app.MapControllers();

app.Run();
