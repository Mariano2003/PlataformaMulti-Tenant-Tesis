using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/negocios/{slug}/pedidos")]
public class PedidosPublicosController : ControllerBase
{
    private readonly INegocioService _negocios;
    private readonly IPedidoService _pedidos;
    private readonly IMercadoPagoPreferenciaService _mercadoPagoPreferencias;
    private readonly IMercadoPagoConfirmacionService _mercadoPagoConfirmacion;

    public PedidosPublicosController(
        INegocioService negocios,
        IPedidoService pedidos,
        IMercadoPagoPreferenciaService mercadoPagoPreferencias,
        IMercadoPagoConfirmacionService mercadoPagoConfirmacion)
    {
        _negocios = negocios;
        _pedidos = pedidos;
        _mercadoPagoPreferencias = mercadoPagoPreferencias;
        _mercadoPagoConfirmacion = mercadoPagoConfirmacion;
    }

    /// <summary>
    /// Crea un pedido en <see cref="PedidoEstados.PendientePago"/>: valida stock disponible y snapshot de líneas; el stock se descuenta al aprobar el pago (webhook).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> Crear(string slug, [FromBody] CrearPedidoRequest solicitud, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        try
        {
            var pedidoCreado = await _pedidos.CrearPendienteDePagoAsync(negocio.Id, solicitud, ct);
            return CreatedAtAction(nameof(PedidosAdminController.PorId), "PedidosAdmin", new { slug = negocio.Slug, id = pedidoCreado.Id }, Map(pedidoCreado));
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

    /// <summary>
    /// Tras volver de Checkout Pro: consulta el pago en MP y confirma el pedido (idempotente).
    /// Respaldo si el webhook no llegó (p. ej. Render, firma incorrecta).
    /// </summary>
    [HttpPost("mercadopago/confirmar-retorno")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConfirmarPagoRetornoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmarPagoRetornoResponse>> ConfirmarRetornoMercadoPago(
        string slug,
        [FromBody] ConfirmarPagoRetornoRequest solicitud,
        CancellationToken ct)
    {
        try
        {
            var dto = await _mercadoPagoConfirmacion.ConfirmarRetornoCheckoutAsync(slug, solicitud, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no existe", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Crea preferencia Checkout Pro y devuelve la URL de pago (sandbox o prod según credenciales).</summary>
    [HttpPost("{pedidoId}/mercadopago/preferencia")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PreferenciaMercadoPagoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreferenciaMercadoPagoResponse>> CrearPreferenciaMercadoPago(
        string slug,
        string pedidoId,
        CancellationToken ct)
    {
        try
        {
            var dto = await _mercadoPagoPreferencias.CrearPreferenciaCheckoutProAsync(slug, pedidoId, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no existe", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
