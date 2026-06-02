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
    private readonly IMercadoPagoOAuthService _oauth;

    public NegocioAdminMercadoPagoController(INegocioService negocios, IMercadoPagoOAuthService oauth)
    {
        _negocios = negocios;
        _oauth = oauth;
    }

    /// <summary>Inicia OAuth Connect: devuelve la URL a la que debe ir el dueño de la tienda.</summary>
    [HttpPost("mercadopago/oauth/iniciar")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(MercadoPagoOAuthIniciarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MercadoPagoOAuthIniciarResponse>> IniciarOAuthConnect(CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        if (!_oauth.ConnectHabilitado)
        {
            return BadRequest(new
            {
                error =
                    "OAuth Connect no está configurado en la API (MercadoPago:OAuthClientId, OAuthClientSecret, PublicApiBaseUrl).",
            });
        }

        try
        {
            var url = await _oauth.IniciarAutorizacionAsync(negocio.Id, negocio.Slug, ct);
            return Ok(new MercadoPagoOAuthIniciarResponse { AuthorizationUrl = url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

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
