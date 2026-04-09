using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface IProductoService
{
    Task<IReadOnlyList<Producto>> ListarPorNegocioAsync(
        string negocioId,
        bool soloActivos,
        string? categoriaId,
        CancellationToken ct = default);

    Task<Producto?> ObtenerPorIdYNegocioAsync(string id, string negocioId, bool soloActivos, CancellationToken ct = default);

    Task<Producto> CrearAsync(string negocioId, CrearProductoRequest dto, CancellationToken ct = default);

    Task<Producto?> ActualizarAsync(string negocioId, string id, ActualizarProductoRequest dto, CancellationToken ct = default);

    Task<bool> EliminarAsync(string negocioId, string id, CancellationToken ct = default);
}
