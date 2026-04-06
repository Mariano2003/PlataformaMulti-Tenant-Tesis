using MarketSaaS.Api.DTOs;
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

    /// <summary>Alta de negocio (tenant). En producción restringir a SuperAdmin.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NegocioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
