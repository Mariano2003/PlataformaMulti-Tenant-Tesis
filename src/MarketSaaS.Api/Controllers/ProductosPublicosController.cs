using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/productos")]
public class ProductosPublicosController : ControllerBase
{
    private readonly INegocioService _negocios;
    private readonly IProductoService _productos;

    public ProductosPublicosController(INegocioService negocios, IProductoService productos)
    {
        _negocios = negocios;
        _productos = productos;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductoResponse>>> Listar(
        string slug,
        [FromQuery] string? categoriaId,
        CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        var list = await _productos.ListarPorNegocioAsync(negocio.Id, soloActivos: true, categoriaId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> PorId(string slug, string id, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        var p = await _productos.ObtenerPorIdYNegocioAsync(id, negocio.Id, soloActivos: true, ct);
        if (p is null)
            return NotFound();

        return Ok(Map(p));
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
