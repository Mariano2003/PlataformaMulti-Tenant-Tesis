using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/productos")]
public class ProductosAdminController : ControllerBase
{
    private readonly IProductoService _productos;

    public ProductosAdminController(IProductoService productos) => _productos = productos;

    [HttpGet]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductoResponse>>> Listar(
        [FromQuery] string? categoriaId,
        CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        var list = await _productos.ListarPorNegocioAsync(negocio.Id, soloActivos: false, categoriaId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> Crear([FromBody] CrearProductoRequest dto, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        try
        {
            var p = await _productos.CrearAsync(negocio.Id, dto, ct);
            return CreatedAtAction(nameof(PorId), new { slug = negocio.Slug, id = p.Id }, Map(p));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> PorId(string id, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        var p = await _productos.ObtenerPorIdYNegocioAsync(id, negocio.Id, soloActivos: false, ct);
        if (p is null)
            return NotFound();

        return Ok(Map(p));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> Actualizar(string id, [FromBody] ActualizarProductoRequest dto, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        try
        {
            var p = await _productos.ActualizarAsync(negocio.Id, id, dto, ct);
            if (p is null)
                return NotFound();
            return Ok(Map(p));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(string id, CancellationToken ct)
    {
        if (!TryNegocio(out var negocio))
            return NotFound();

        var ok = await _productos.EliminarAsync(negocio.Id, id, ct);
        if (!ok)
            return NotFound();
        return NoContent();
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

    private static ProductoResponse Map(Producto p) => new()
    {
        Id = p.Id,
        NegocioId = p.NegocioId,
        CategoriaId = p.CategoriaId,
        Nombre = p.Nombre,
        DescripcionCorta = p.DescripcionCorta,
        Precio = p.Precio,
        Stock = p.Stock,
        Atributos = p.Atributos,
        Activo = p.Activo,
        CreadoEn = p.CreadoEn,
    };
}
