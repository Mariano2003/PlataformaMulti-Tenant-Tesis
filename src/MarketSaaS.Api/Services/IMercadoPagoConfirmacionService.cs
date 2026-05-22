using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

/// <summary>
/// Confirma pagos al volver del Checkout Pro (respaldo si el webhook no llegó o falló la firma).
/// </summary>
public interface IMercadoPagoConfirmacionService
{
    Task<ConfirmarPagoRetornoResponse> ConfirmarRetornoCheckoutAsync(
        string slugNegocio,
        ConfirmarPagoRetornoRequest solicitud,
        CancellationToken ct = default);
}
