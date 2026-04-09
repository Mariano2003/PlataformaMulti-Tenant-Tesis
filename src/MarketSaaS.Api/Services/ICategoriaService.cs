using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<Categoria>> ListarPorNegocioAsync(string negocioId, bool soloActivos, CancellationToken ct = default);

    Task<Categoria?> ObtenerPorIdYNegocioAsync(string id, string negocioId, CancellationToken ct = default);

    Task<Categoria> CrearAsync(string negocioId, CrearCategoriaRequest dto, CancellationToken ct = default);

    Task<Categoria?> ActualizarAsync(string negocioId, string id, ActualizarCategoriaRequest dto, CancellationToken ct = default);

    Task<bool> EliminarAsync(string negocioId, string id, CancellationToken ct = default);
}
