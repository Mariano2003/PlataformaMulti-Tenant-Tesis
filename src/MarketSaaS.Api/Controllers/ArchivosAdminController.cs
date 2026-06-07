using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/archivos")]
public sealed class ArchivosAdminController : ControllerBase
{
    private readonly IImageStorageService _storage;
    private readonly MercadoPagoOptions _mp;

    public ArchivosAdminController(IImageStorageService storage, IOptions<MercadoPagoOptions> mp)
    {
        _storage = storage;
        _mp = mp.Value;
    }

    [HttpPost("imagen")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(ImagenSubidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImagenSubidaResponse>> SubirImagen(
        IFormFile archivo,
        CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { error = "No se recibió ningún archivo." });

        try
        {
            await using var stream = archivo.OpenReadStream();
            var rutaRelativa = await _storage.GuardarImagenProductoAsync(
                negocio.Id,
                stream,
                archivo.ContentType,
                archivo.FileName,
                ct);

            var urlPublica = ConstruirUrlPublica(rutaRelativa);
            return Ok(new ImagenSubidaResponse { Url = urlPublica });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string ConstruirUrlPublica(string rutaRelativa)
    {
        var apiBase = _mp.PublicApiBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(apiBase))
            return $"{apiBase}{rutaRelativa}";

        var req = HttpContext.Request;
        return $"{req.Scheme}://{req.Host}{rutaRelativa}";
    }
}
