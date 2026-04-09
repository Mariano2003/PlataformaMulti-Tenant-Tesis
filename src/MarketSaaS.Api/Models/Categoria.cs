using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public class Categoria
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("negocioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string NegocioId { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("orden")]
    public int Orden { get; set; }

    [BsonElement("activo")]
    public bool Activo { get; set; } = true;

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
