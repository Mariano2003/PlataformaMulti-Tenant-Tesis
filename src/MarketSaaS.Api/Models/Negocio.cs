using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public class Negocio
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("slug")]
    public string Slug { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("descripcionCorta")]
    public string? DescripcionCorta { get; set; }

    [BsonElement("logoUrl")]
    public string? LogoUrl { get; set; }

    [BsonElement("temaJson")]
    public string? TemaJson { get; set; }

    [BsonElement("emailContacto")]
    public string? EmailContacto { get; set; }

    /// <summary>Access Token de la aplicación MP del vendedor (tienda). Si es null, se usa el de <c>MercadoPagoOptions</c> en la API.</summary>
    [BsonElement("mercadoPagoAccessToken")]
    public string? MercadoPagoAccessToken { get; set; }

    /// <summary>Clave secreta del webhook configurada en el panel MP de esa cuenta (opcional; si hay valor se valida la firma con ella).</summary>
    [BsonElement("mercadoPagoWebhookSecret")]
    public string? MercadoPagoWebhookSecret { get; set; }

    [BsonElement("activo")]
    public bool Activo { get; set; } = true;

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
