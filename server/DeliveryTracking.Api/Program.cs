using DeliveryTracking.Api.Data.Mongo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMongo();

var app = builder.Build();

app.MapControllers();

app.Run();
