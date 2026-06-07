using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
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
    [ProducesResponseType(typeof(PaginaResponse<ProductoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResponse<ProductoResponse>>> Listar(
        string slug,
        [FromQuery] string? categoriaId,
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 12,
        CancellationToken ct = default)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        var (items, total) = await _productos.ListarPorNegocioPaginadoAsync(
            negocio.Id, soloActivos: true, categoriaId, buscar, pagina, tamano, ct);
        var (p, t, _) = PaginacionConsulta.Normalizar(pagina, tamano, tamanoMaximo: 48);
        var respuesta = PaginacionConsulta.Armar(items.Select(Map).ToList(), p, t, total);
        return Ok(respuesta);
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

        var producto = await _productos.ObtenerPorIdYNegocioAsync(id, negocio.Id, soloActivos: true, ct);
        if (producto is null)
            return NotFound();

        return Ok(Map(producto));
    }

    private static ProductoResponse Map(Producto producto) => new()
    {
        Id = producto.Id,
        NegocioId = producto.NegocioId,
        CategoriaId = producto.CategoriaId,
        Nombre = producto.Nombre,
        DescripcionCorta = producto.DescripcionCorta,
        ImagenUrl = producto.ImagenUrl,
        Precio = producto.Precio,
        Stock = producto.Stock,
        Atributos = producto.Atributos,
        Activo = producto.Activo,
        CreadoEn = producto.CreadoEn,
    };
}
