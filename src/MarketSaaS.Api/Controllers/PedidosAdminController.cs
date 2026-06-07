using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/admin/pedidos")]
public class PedidosAdminController : ControllerBase
{
    private readonly IPedidoService _pedidos;

    public PedidosAdminController(IPedidoService pedidos) => _pedidos = pedidos;

    [HttpGet]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(PaginaResponse<PedidoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResponse<PedidoResponse>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var (items, total) = await _pedidos.ListarPorNegocioPaginadoAsync(negocio.Id, pagina, tamano, ct);
        var (p, t, _) = PaginacionConsulta.Normalizar(pagina, tamano);
        var respuesta = PaginacionConsulta.Armar(items.Select(Map).ToList(), p, t, total);
        return Ok(respuesta);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> PorId(string id, CancellationToken ct)
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        var pedido = await _pedidos.ObtenerPorIdYNegocioAsync(id, negocio.Id, ct);
        if (pedido is null)
            return NotFound();

        return Ok(Map(pedido));
    }

    private static PedidoResponse Map(Pedido pedido) => new()
    {
        Id = pedido.Id,
        NegocioId = pedido.NegocioId,
        Estado = pedido.Estado,
        MercadoPagoPreferenceId = pedido.MercadoPagoPreferenceId,
        MercadoPagoPaymentId = pedido.MercadoPagoPaymentId,
        MercadoPagoStatusDetail = pedido.MercadoPagoStatusDetail,
        Lineas = pedido.Lineas.Select(linea => new PedidoLineaResponse
        {
            ProductoId = linea.ProductoId,
            Nombre = linea.Nombre,
            Cantidad = linea.Cantidad,
            PrecioUnitario = linea.PrecioUnitario,
            Subtotal = linea.Subtotal,
        }).ToList(),
        Total = pedido.Total,
        ClienteNombre = pedido.ClienteNombre,
        ClienteEmail = pedido.ClienteEmail,
        ClienteTelefono = pedido.ClienteTelefono,
        CreadoEn = pedido.CreadoEn,
    };
}
