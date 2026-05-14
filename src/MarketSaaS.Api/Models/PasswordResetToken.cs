using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public sealed class PasswordResetToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("email")]
    public string EmailNormalizado { get; set; } = null!;

    /// <summary>SHA-256 hex en minúsculas del token en texto plano.</summary>
    [BsonElement("tokenHash")]
    public string TokenHash { get; set; } = null!;

    [BsonElement("expiraEnUtc")]
    public DateTime ExpiraEnUtc { get; set; }

    [BsonElement("creadoEnUtc")]
    public DateTime CreadoEnUtc { get; set; }
}
