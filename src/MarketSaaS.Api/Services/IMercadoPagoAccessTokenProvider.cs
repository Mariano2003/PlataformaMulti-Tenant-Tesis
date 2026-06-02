using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

/// <summary>Resuelve el access token efectivo (tienda OAuth/manual o global) y renueva si corresponde.</summary>
public interface IMercadoPagoAccessTokenProvider
{
    Task<string?> ObtenerParaNegocioAsync(Negocio negocio, CancellationToken ct = default);
}
