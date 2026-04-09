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

        var list = await _categorias.ListarPorNegocioAsync(negocio.Id, soloActivos: true, ct);
        return Ok(list.Select(Map).ToList());
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

        var c = await _categorias.ObtenerPorIdYNegocioAsync(id, negocio.Id, ct);
        if (c is null || !c.Activo)
            return NotFound();

        return Ok(Map(c));
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
