using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MercadoPago.Client;
using MercadoPago.Client.MerchantOrder;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.MerchantOrder;
using MercadoPago.Resource.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class MercadoPagoConfirmacionService : IMercadoPagoConfirmacionService
{
    private readonly INegocioService _negocios;
    private readonly IPedidoService _pedidos;
    private readonly MercadoPagoOptions _opciones;
    private readonly ILogger<MercadoPagoConfirmacionService> _log;

    public MercadoPagoConfirmacionService(
        INegocioService negocios,
        IPedidoService pedidos,
        IOptions<MercadoPagoOptions> opciones,
        ILogger<MercadoPagoConfirmacionService> log)
    {
        _negocios = negocios;
        _pedidos = pedidos;
        _opciones = opciones.Value;
        _log = log;
    }

    public async Task<ConfirmarPagoRetornoResponse> ConfirmarRetornoCheckoutAsync(
        string slugNegocio,
        ConfirmarPagoRetornoRequest solicitud,
        CancellationToken ct = default)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slugNegocio, ct)
            ?? throw new InvalidOperationException("El negocio no existe.");

        var accessToken = !string.IsNullOrWhiteSpace(negocio.MercadoPagoAccessToken)
            ? negocio.MercadoPagoAccessToken.Trim()
            : _opciones.AccessToken?.Trim();

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Mercado Pago no está configurado para esta tienda.");

        var opts = new RequestOptions { AccessToken = accessToken };

        if (TryParseIdPago(solicitud.PaymentId, out var idPago)
            || TryParseIdPago(solicitud.CollectionId, out idPago))
        {
            return await ConfirmarPorPagoIdAsync(negocio, idPago, opts, ct);
        }

        if (TryParseIdPago(solicitud.MerchantOrderId, out var idOrden))
        {
            return await ConfirmarPorMerchantOrderAsync(negocio, idOrden, opts, ct);
        }

        var pedidoId = solicitud.ExternalReference?.Trim();
        if (!string.IsNullOrEmpty(pedidoId))
        {
            var pedido = await _pedidos.ObtenerPorIdYNegocioAsync(pedidoId, negocio.Id, ct);
            if (pedido is null)
                return Respuesta(false, pedidoId, null, "No se encontró el pedido en esta tienda.");

            if (pedido.Estado == PedidoEstados.Pagado)
                return Respuesta(true, pedido.Id, pedido.Estado, "El pedido ya estaba pagado.");

            return Respuesta(
                false,
                pedido.Id,
                pedido.Estado,
                "Faltan payment_id o merchant_order_id en la URL de retorno. Revisá el webhook en Mercado Pago.");
        }

        return Respuesta(false, null, null, "No hay datos de pago para confirmar.");
    }

    private async Task<ConfirmarPagoRetornoResponse> ConfirmarPorMerchantOrderAsync(
        Models.Negocio negocio,
        long merchantOrderId,
        RequestOptions opts,
        CancellationToken ct)
    {
        var clienteOrden = new MerchantOrderClient();
        MerchantOrder orden = await clienteOrden.GetAsync(merchantOrderId, opts, ct);

        if (orden.Payments is null || orden.Payments.Count == 0)
            return Respuesta(false, null, null, "La orden de Mercado Pago no tiene pagos asociados.");

        ConfirmarPagoRetornoResponse? ultimo = null;
        var clientePago = new PaymentClient();
        foreach (var refPago in orden.Payments)
        {
            if (refPago?.Id is null)
                continue;
            var pago = await clientePago.GetAsync(refPago.Id.Value, opts, ct);
            ultimo = await ProcesarPagoAsync(negocio, pago, ct);
            if (ultimo.Procesado)
                return ultimo;
        }

        return ultimo ?? Respuesta(false, null, null, "No se pudo confirmar ningún pago de la orden.");
    }

    private async Task<ConfirmarPagoRetornoResponse> ConfirmarPorPagoIdAsync(
        Models.Negocio negocio,
        long paymentId,
        RequestOptions opts,
        CancellationToken ct)
    {
        var cliente = new PaymentClient();
        Payment pago = await cliente.GetAsync(paymentId, opts, ct);
        return await ProcesarPagoAsync(negocio, pago, ct);
    }

    private async Task<ConfirmarPagoRetornoResponse> ProcesarPagoAsync(
        Models.Negocio negocio,
        Payment pago,
        CancellationToken ct)
    {
        var pedidoId = pago.ExternalReference?.Trim();
        if (string.IsNullOrEmpty(pedidoId))
            return Respuesta(false, null, null, "El pago en Mercado Pago no tiene referencia al pedido.");

        var pedido = await _pedidos.ObtenerPorIdYNegocioAsync(pedidoId, negocio.Id, ct);
        if (pedido is null)
        {
            _log.LogWarning(
                "Retorno MP pago {PaymentId} referencia pedido {PedidoId} que no pertenece a negocio {NegocioId}",
                pago.Id,
                pedidoId,
                negocio.Id);
            return Respuesta(false, pedidoId, null, "El pedido no pertenece a esta tienda.");
        }

        var accion = MercadoPagoPaymentWebhookClassifier.Clasificar(pago.Status);
        var idPago = pago.Id?.ToString() ?? "";

        switch (accion)
        {
            case MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Aprobado:
                await _pedidos.ProcesarPagoAprobadoMercadoPagoAsync(pedidoId, idPago, ct);
                var pagado = await _pedidos.ObtenerPorIdYNegocioAsync(pedidoId, negocio.Id, ct);
                _log.LogInformation(
                    "Pago confirmado por retorno checkout: pedido {PedidoId} MP {PaymentId} estado {Estado}",
                    pedidoId,
                    pago.Id,
                    pagado?.Estado);
                var ok = pagado?.Estado == PedidoEstados.Pagado;
                return Respuesta(
                    ok,
                    pedidoId,
                    pagado?.Estado,
                    ok
                        ? "Pago confirmado y stock actualizado."
                        : "El pago está aprobado en Mercado Pago pero el pedido no pasó a Pagado (revisá stock disponible).");

            case MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal:
                await _pedidos.MarcarPedidoRechazadoSiPendienteMercadoPagoAsync(
                    pedidoId,
                    idPago,
                    pago.StatusDetail,
                    ct);
                return Respuesta(
                    true,
                    pedidoId,
                    PedidoEstados.Rechazado,
                    "El pago fue rechazado en Mercado Pago.");

            default:
                return Respuesta(
                    false,
                    pedidoId,
                    pedido.Estado,
                    $"El pago sigue en estado «{pago.Status}» en Mercado Pago. Esperá unos segundos o revisá el webhook.");
        }
    }

    private static bool TryParseIdPago(string? raw, out long id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return long.TryParse(raw.Trim(), out id) && id > 0;
    }

    private static ConfirmarPagoRetornoResponse Respuesta(
        bool procesado,
        string? pedidoId,
        string? estado,
        string mensaje) =>
        new()
        {
            Procesado = procesado,
            PedidoId = pedidoId,
            EstadoPedido = estado,
            Mensaje = mensaje,
        };
}
