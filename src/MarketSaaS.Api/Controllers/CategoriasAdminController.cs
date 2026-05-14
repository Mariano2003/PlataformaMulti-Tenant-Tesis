using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/categorias")]
public class CategoriasAdminController : ControllerBase
{
    private readonly ICategoriaService _categorias;

    public CategoriasAdminController(ICategoriaService categorias) => _categorias = categorias;

    [HttpGet]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var categorias = await _categorias.ListarPorNegocioAsync(negocio.Id, soloActivos: false, ct);
        return Ok(categorias.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaResponse>> Crear([FromBody] CrearCategoriaRequest solicitud, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        try
        {
            var categoriaCreada = await _categorias.CrearAsync(negocio.Id, solicitud, ct);
            return CreatedAtAction(nameof(PorId), new { slug = negocio.Slug, id = categoriaCreada.Id }, Map(categoriaCreada));
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

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaResponse>> PorId(string id, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var categoria = await _categorias.ObtenerPorIdYNegocioAsync(id, negocio.Id, ct);
        if (categoria is null)
            return NotFound();

        return Ok(Map(categoria));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaResponse>> Actualizar(string id, [FromBody] ActualizarCategoriaRequest solicitud, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        try
        {
            var categoriaActualizada = await _categorias.ActualizarAsync(negocio.Id, id, solicitud, ct);
            if (categoriaActualizada is null)
                return NotFound();
            return Ok(Map(categoriaActualizada));
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

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(string id, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        try
        {
            var eliminado = await _categorias.EliminarAsync(negocio.Id, id, ct);
            if (!eliminado)
                return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private static CategoriaResponse Map(Categoria categoria) => new()
    {
        Id = categoria.Id,
        NegocioId = categoria.NegocioId,
        Nombre = categoria.Nombre,
        Orden = categoria.Orden,
        Activo = categoria.Activo,
        CreadoEn = categoria.CreadoEn,
    };
}
