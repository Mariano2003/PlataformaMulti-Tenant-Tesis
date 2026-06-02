using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/mercadopago/oauth")]
public sealed class MercadoPagoOAuthController : ControllerBase
{
    private readonly IMercadoPagoOAuthService _oauth;
    private readonly MercadoPagoOptions _mp;
    private readonly EmailOptions _email;

    public MercadoPagoOAuthController(
        IMercadoPagoOAuthService oauth,
        IOptions<MercadoPagoOptions> mp,
        IOptions<EmailOptions> email)
    {
        _oauth = oauth;
        _mp = mp.Value;
        _email = email.Value;
    }

    /// <summary>Callback público de Mercado Pago tras autorizar la app de la plataforma.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken ct)
    {
        var appBase = ObtenerUrlBaseFront();
        var slugFallback = "admin";

        if (!string.IsNullOrWhiteSpace(error))
        {
            var msg = error_description ?? error;
            return Redirect(ConstruirUrlAdmin(appBase, slugFallback, "error", msg));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return Redirect(ConstruirUrlAdmin(appBase, slugFallback, "error", "Faltan parámetros de Mercado Pago."));

        try
        {
            var slug = await _oauth.CompletarAutorizacionAsync(code, state, ct);
            return Redirect(ConstruirUrlAdmin(appBase, slug, "ok", null));
        }
        catch (Exception ex)
        {
            return Redirect(ConstruirUrlAdmin(appBase, slugFallback, "error", ex.Message));
        }
    }

    private static string ConstruirUrlAdmin(string appBase, string slug, string resultado, string? mensaje)
    {
        var query = new Dictionary<string, string?> { ["mp_oauth"] = resultado };
        if (!string.IsNullOrWhiteSpace(mensaje))
            query["mp_msg"] = mensaje.Length > 200 ? mensaje[..200] : mensaje;

        var ruta = QueryHelpers.AddQueryString($"/admin/{Uri.EscapeDataString(slug)}/mercadopago", query!);
        return FrontAppUrls.Construir(appBase, ruta);
    }

    private string ObtenerUrlBaseFront()
    {
        var app = _mp.PublicAppBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(app))
            return FrontAppUrls.NormalizarBase(app);

        var email = _email.PublicAppBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            return FrontAppUrls.NormalizarBase(email);

        throw new InvalidOperationException(
            "Configurá MercadoPago:PublicAppBaseUrl o Email:PublicAppBaseUrl para redirigir tras OAuth.");
    }
}
