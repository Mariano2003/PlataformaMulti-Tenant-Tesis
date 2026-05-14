using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public class Producto
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("negocioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string NegocioId { get; set; } = null!;

    /// <summary>Opcional; debe pertenecer al mismo negocio.</summary>
    [BsonElement("categoriaId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoriaId { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("descripcionCorta")]
    public string? DescripcionCorta { get; set; }

    /// <summary>URL pública de imagen (hosting externo o CDN).</summary>
    [BsonElement("imagenUrl")]
    public string? ImagenUrl { get; set; }

    [BsonElement("precio")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Precio { get; set; }

    [BsonElement("stock")]
    public int Stock { get; set; }

    /// <summary>Atributos variables (talle, color, etc.).</summary>
    [BsonElement("atributos")]
    public Dictionary<string, string>? Atributos { get; set; }

    [BsonElement("activo")]
    public bool Activo { get; set; } = true;

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
