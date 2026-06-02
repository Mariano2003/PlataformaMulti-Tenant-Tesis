using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

/// <summary>Estado temporal del flujo OAuth Connect (authorization code).</summary>
public sealed class MercadoPagoOAuthState
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("state")]
    public string State { get; set; } = null!;

    [BsonElement("negocioId")]
    public string NegocioId { get; set; } = null!;

    [BsonElement("slug")]
    public string Slug { get; set; } = null!;

    /// <summary>Verifier PKCE; null si OAuthUsePkce está deshabilitado.</summary>
    [BsonElement("codeVerifier")]
    public string? CodeVerifier { get; set; }

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [BsonElement("expiraEn")]
    public DateTime ExpiraEn { get; set; }
}
