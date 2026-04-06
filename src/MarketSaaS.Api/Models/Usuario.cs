using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>Null para SuperAdmin de plataforma.</summary>
    [BsonElement("negocioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? NegocioId { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("apellido")]
    public string? Apellido { get; set; }

    [BsonElement("telefono")]
    public string? Telefono { get; set; }

    [BsonElement("rol")]
    public string Rol { get; set; } = null!;

    [BsonElement("activo")]
    public bool Activo { get; set; } = true;

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
