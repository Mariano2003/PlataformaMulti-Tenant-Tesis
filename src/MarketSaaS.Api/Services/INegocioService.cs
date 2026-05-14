using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface INegocioService
{
    Task<Negocio?> ObtenerPorIdAsync(string id, CancellationToken ct = default);
    Task<Negocio?> ObtenerPorSlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Negocio>> ListarActivosOrdenadosAsync(CancellationToken ct = default);
    Task<Negocio> CrearAsync(CrearNegocioRequest dto, CancellationToken ct = default);

    /// <summary>Elimina por id (uso interno p. ej. rollback).</summary>
    Task EliminarPorIdAsync(string id, CancellationToken ct = default);

    Task ActualizarMercadoPagoAsync(string negocioId, ActualizarMercadoPagoNegocioRequest dto, CancellationToken ct = default);
}
