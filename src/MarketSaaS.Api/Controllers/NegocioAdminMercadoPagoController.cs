using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin")]
public sealed class NegocioAdminMercadoPagoController : ControllerBase
{
    private readonly INegocioService _negocios;

    public NegocioAdminMercadoPagoController(INegocioService negocios) => _negocios = negocios;

    /// <summary>
    /// Credenciales Mercado Pago de la tienda: los cobros van a la cuenta del vendedor (Access Token de su aplicación MP).
    /// Si no configurás nada aquí, se usa <c>MercadoPago:AccessToken</c> global de la API (modo plataforma única).
    /// </summary>
    [HttpPut("mercadopago")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActualizarMercadoPagoTienda(
        [FromBody] ActualizarMercadoPagoNegocioRequest solicitud,
        CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        await _negocios.ActualizarMercadoPagoAsync(negocio.Id, solicitud, ct);
        return Ok(new { mensaje = "Configuración de Mercado Pago actualizada." });
    }
}
