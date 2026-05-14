using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/analytics")]
public class AnalyticsAdminController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsAdminController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("resumen")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(VentasResumenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VentasResumenResponse>> Resumen(CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var resumen = await _analytics.ObtenerResumenPedidosAsync(negocio.Id, ct);
        return Ok(resumen);
    }
}
