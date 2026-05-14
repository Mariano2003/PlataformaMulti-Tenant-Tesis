using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/categorias")]
public class CategoriasPublicasController : ControllerBase
{
    private readonly INegocioService _negocios;
    private readonly ICategoriaService _categorias;

    public CategoriasPublicasController(INegocioService negocios, ICategoriaService categorias)
    {
        _negocios = negocios;
        _categorias = categorias;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(string slug, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        var categorias = await _categorias.ListarPorNegocioAsync(negocio.Id, soloActivos: true, ct);
        return Ok(categorias.Select(Map).ToList());
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaResponse>> PorId(string slug, string id, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        var categoria = await _categorias.ObtenerPorIdYNegocioAsync(id, negocio.Id, ct);
        if (categoria is null || !categoria.Activo)
            return NotFound();

        return Ok(Map(categoria));
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
