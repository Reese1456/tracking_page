using System.ComponentModel.DataAnnotations;

namespace DeliveryTracking.Api.Data.Mongo;

public sealed class MongoSettings
{
    public const string SectionName = "Mongo";

    [Required(ErrorMessage =
        "Mongo:ConnectionString is missing. Locally, run: dotnet user-secrets set \"Mongo:ConnectionString\" \"<connection string>\". " +
        "In production, set the Mongo__ConnectionString environment variable.")]
    public string ConnectionString { get; set; } = "";

    [Required(ErrorMessage = "Mongo:DatabaseName is missing. Set it in appsettings.json.")]
    public string DatabaseName { get; set; } = "";
}
