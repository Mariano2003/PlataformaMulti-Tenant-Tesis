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
        if (!TryNegocio(out var negocio))
            return NotFound();

        var list = await _categorias.ListarPorNegocioAsync(negocio.Id, soloActivos: false, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaResponse>> Crear([FromBody] CrearCategoriaRequest dto, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        try
        {
            var c = await _categorias.CrearAsync(negocio.Id, dto, ct);
            return CreatedAtAction(nameof(PorId), new { slug = negocio.Slug, id = c.Id }, Map(c));
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
        if (!TryNegocio(out var negocio))
            return NotFound();

        var c = await _categorias.ObtenerPorIdYNegocioAsync(id, negocio.Id, ct);
        if (c is null)
            return NotFound();

        return Ok(Map(c));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaResponse>> Actualizar(string id, [FromBody] ActualizarCategoriaRequest dto, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        try
        {
            var c = await _categorias.ActualizarAsync(negocio.Id, id, dto, ct);
            if (c is null)
                return NotFound();
            return Ok(Map(c));
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
        if (!TryNegocio(out var negocio))
            return NotFound();

        try
        {
            var ok = await _categorias.EliminarAsync(negocio.Id, id, ct);
            if (!ok)
                return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private bool TryNegocio(out Negocio negocio)
    {
        if (HttpContext.Items.TryGetValue(HttpContextItemKeys.NegocioActual, out var raw) && raw is Negocio n)
        {
            negocio = n;
            return true;
        }

        negocio = null!;
        return false;
    }

    private static CategoriaResponse Map(Categoria c) => new()
    {
        Id = c.Id,
        NegocioId = c.NegocioId,
        Nombre = c.Nombre,
        Orden = c.Orden,
        Activo = c.Activo,
        CreadoEn = c.CreadoEn,
    };
}
