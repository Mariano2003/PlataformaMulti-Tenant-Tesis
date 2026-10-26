using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Controllers;

/// <summary>
/// Puente de retorno de Checkout Pro. Mercado Pago no acepta back_urls con <c>#</c>
/// (el router hash del front); esta ruta recibe el pago y redirige al SPA.
/// </summary>
[ApiController]
[Route("api/mercadopago/retorno")]
public sealed class MercadoPagoRetornoController : ControllerBase
{
    private readonly MercadoPagoOptions _mp;
    private readonly EmailOptions _email;

    public MercadoPagoRetornoController(IOptions<MercadoPagoOptions> mp, IOptions<EmailOptions> email)
    {
        _mp = mp.Value;
        _email = email.Value;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Retorno([FromQuery] string? slug, [FromQuery] string? pago)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest("Falta el slug de la tienda.");

        var resultado = pago is "ok" or "error" or "pending" ? pago : "pending";
        var query = Request.Query
            .Where(q => !string.Equals(q.Key, "slug", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(q.Key, "pago", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(q => q.Key, q => (string?)q.Value.ToString());
        query["pago"] = resultado;

        var ruta = QueryHelpers.AddQueryString(
            $"/tienda/{Uri.EscapeDataString(slug.Trim())}",
            query!);

        var front = FrontAppUrls.Construir(ObtenerUrlBaseFront(), ruta, _mp.SpaUseHashRouter);
        return Redirect(front);
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
            "Configurá MercadoPago:PublicAppBaseUrl para volver a la tienda tras el pago.");
    }
}
