using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface INegocioService
{
    Task<Negocio?> ObtenerPorSlugAsync(string slug, CancellationToken ct = default);
    Task<Negocio> CrearAsync(CrearNegocioRequest dto, CancellationToken ct = default);
}
