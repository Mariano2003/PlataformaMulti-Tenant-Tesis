using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

public interface IAnalyticsService
{
    Task<VentasResumenResponse> ObtenerResumenPedidosAsync(string negocioId, CancellationToken ct = default);
}
