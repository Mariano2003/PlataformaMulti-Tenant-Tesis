using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NegociosController : ControllerBase
{
    private readonly INegocioService _negocios;

    public NegociosController(INegocioService negocios) => _negocios = negocios;

    /// <summary>Obtiene un negocio por su slug (URL pública).</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NegocioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioResponse>> PorSlug(string slug, CancellationToken ct)
    {
        var n = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (n is null)
            return NotFound();

        return Ok(ToResponse(n));
    }

    /// <summary>
    /// Verifica JWT + que el <c>negocio_id</c> del token corresponda al <paramref name="slug"/> (SuperAdmin puede cualquier slug existente).
    /// Plantilla para futuros <c>.../admin/...</c>.
    /// </summary>
    [HttpGet("{slug}/admin/contexto")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(NegocioContextoAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<NegocioContextoAdminResponse> ContextoAdmin()
    {
        if (!HttpContext.Items.TryGetValue(HttpContextItemKeys.NegocioActual, out var raw) || raw is not Negocio n)
            return NotFound();

        return Ok(new NegocioContextoAdminResponse
        {
            NegocioId = n.Id,
            Slug = n.Slug,
            Nombre = n.Nombre,
            Activo = n.Activo,
        });
    }

    /// <summary>Alta de negocio (tenant). Solo <see cref="Roles.SuperAdmin"/>.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(typeof(NegocioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NegocioResponse>> Crear([FromBody] CrearNegocioRequest dto, CancellationToken ct)
    {
        try
        {
            var n = await _negocios.CrearAsync(dto, ct);
            return CreatedAtAction(nameof(PorSlug), new { slug = n.Slug }, ToResponse(n));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private static NegocioResponse ToResponse(Models.Negocio n) => new()
    {
        Id = n.Id,
        Slug = n.Slug,
        Nombre = n.Nombre,
        DescripcionCorta = n.DescripcionCorta,
        LogoUrl = n.LogoUrl,
        Activo = n.Activo,
        CreadoEn = n.CreadoEn,
    };
}
