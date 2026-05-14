using System.Text.Json;
using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/productos")]
public class ProductosAdminController : ControllerBase
{
    private readonly IProductoService _productos;
    private readonly JsonSerializerOptions _json;

    public ProductosAdminController(IProductoService productos, IOptions<JsonOptions> jsonOptions)
    {
        _productos = productos;
        _json = jsonOptions.Value.JsonSerializerOptions;
    }

    [HttpGet]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductoResponse>>> Listar(
        [FromQuery] string? categoriaId,
        CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var productos = await _productos.ListarPorNegocioAsync(negocio.Id, soloActivos: false, categoriaId, ct);
        return Ok(productos.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> Crear([FromBody] JsonElement json, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        CrearProductoRequest solicitud;
        try
        {
            solicitud = json.Deserialize<CrearProductoRequest>(_json)
                ?? throw new JsonException("Cuerpo vacío.");
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "JSON inválido o incompleto." });
        }

        FusionarImagenUrlSiFalta(solicitud, json);

        try
        {
            var productoCreado = await _productos.CrearAsync(negocio.Id, solicitud, ct);
            return CreatedAtAction(nameof(PorId), new { slug = negocio.Slug, id = productoCreado.Id }, Map(productoCreado));
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
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var producto = await _productos.ObtenerPorIdYNegocioAsync(id, negocio.Id, soloActivos: false, ct);
        if (producto is null)
            return NotFound();

        return Ok(Map(producto));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ProductoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoResponse>> Actualizar(string id, [FromBody] JsonElement json, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        ActualizarProductoRequest solicitud;
        try
        {
            solicitud = json.Deserialize<ActualizarProductoRequest>(_json)
                ?? throw new JsonException("Cuerpo vacío.");
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "JSON inválido o incompleto." });
        }

        FusionarImagenUrlSiFalta(solicitud, json);

        try
        {
            var productoActualizado = await _productos.ActualizarAsync(negocio.Id, id, solicitud, ct);
            if (productoActualizado is null)
                return NotFound();
            return Ok(Map(productoActualizado));
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
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var eliminado = await _productos.EliminarAsync(negocio.Id, id, ct);
        if (!eliminado)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// <see cref="JsonElement.TryGetProperty"/> distingue mayúsculas; el binder puede no mapear <c>imagenUrl</c> al CLR <c>ImagenUrl</c> en algunos entornos.
    /// </summary>
    private static void FusionarImagenUrlSiFalta(CrearProductoRequest dto, JsonElement json)
    {
        if (!string.IsNullOrWhiteSpace(dto.ImagenUrl))
            return;
        var url = LeerImagenUrlDeJsonObjeto(json);
        if (!string.IsNullOrWhiteSpace(url))
            dto.ImagenUrl = url;
    }

    private static void FusionarImagenUrlSiFalta(ActualizarProductoRequest dto, JsonElement json)
    {
        if (!string.IsNullOrWhiteSpace(dto.ImagenUrl))
            return;
        var url = LeerImagenUrlDeJsonObjeto(json);
        if (!string.IsNullOrWhiteSpace(url))
            dto.ImagenUrl = url;
    }

    private static string? LeerImagenUrlDeJsonObjeto(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;
        ReadOnlySpan<string> nombres =
        [
            "imagenUrl",
            "ImagenUrl",
            "imagen_url",
            "imageUrl",
            "ImageUrl",
        ];
        foreach (var name in nombres)
        {
            if (!root.TryGetProperty(name, out var el))
                continue;
            if (el.ValueKind != JsonValueKind.String)
                continue;
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        return null;
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
