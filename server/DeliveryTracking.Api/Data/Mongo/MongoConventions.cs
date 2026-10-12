using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace DeliveryTracking.Api.Data.Mongo;

/// <summary>
/// Gives the document classes camelCase field names and string enums without
/// attributes on every property.
/// </summary>
public static class MongoConventions
{
    private static readonly Lock RegisterLock = new();
    private static bool _registered;

    // Conventions are global to the driver and only apply to class maps built
    // after registration, so this must run before the first serialization.
    // Integration tests build the app several times, hence the guard.
    public static void Register()
    {
        lock (RegisterLock)
        {
            if (_registered)
            {
                return;
            }

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true),
            };

            ConventionRegistry.Register(
                "DeliveryTracking",
                pack,
                type => type.Namespace == typeof(ShipmentDocument).Namespace);

            _registered = true;
        }
    }
}
