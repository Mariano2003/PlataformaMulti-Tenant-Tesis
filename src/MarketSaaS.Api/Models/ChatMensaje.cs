using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

/// <summary>Mensaje de chat asociado a un negocio (tenant).</summary>
public class ChatMensaje
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("negocioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string NegocioId { get; set; } = null!;

    /// <summary>Slug normalizado en minúsculas (denormalizado para listados).</summary>
    [BsonElement("slug")]
    public string Slug { get; set; } = null!;

    [BsonElement("remitenteTipo")]
    public string RemitenteTipo { get; set; } = null!;

    [BsonElement("remitenteNombre")]
    public string RemitenteNombre { get; set; } = null!;

    [BsonElement("texto")]
    public string Texto { get; set; } = null!;

    [BsonElement("enviadoEn")]
    public DateTime EnviadoEn { get; set; } = DateTime.UtcNow;
}
