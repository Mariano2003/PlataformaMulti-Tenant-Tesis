using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

public interface IMercadoPagoPreferenciaService
{
    /// <summary>Crea preferencia Checkout Pro para un pedido <c>PendientePago</c> del negocio.</summary>
    Task<PreferenciaMercadoPagoResponse> CrearPreferenciaCheckoutProAsync(
        string slugNegocio,
        string pedidoId,
        CancellationToken ct = default);
}
