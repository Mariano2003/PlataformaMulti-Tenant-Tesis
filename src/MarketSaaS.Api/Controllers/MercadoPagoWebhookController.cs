using System.Text.Json;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using MercadoPago.Client;
using MercadoPago.Client.MerchantOrder;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.MerchantOrder;
using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Controllers;

/// <summary>
/// Notificaciones Mercado Pago: IPN (<c>topic=payment|merchant_order&amp;id=</c>), webhooks JSON (<c>type</c> + <c>data.id</c>).
/// Checkout Pro suele notificar <c>merchant_order</c>; se resuelve la orden, se leen los pagos y se procesa igual que con <c>payment</c>.
/// Pago aprobado → confirma pedido; rechazo terminal → <c>Rechazado</c> si sigue <c>PendientePago</c>.
/// Sin <c>AccessToken</c> (global o por tienda vía query <c>n</c>) responde 200 sin procesar.
/// Si hay secreto de webhook (global o por tienda) exige <c>x-signature</c> + <c>x-request-id</c> válidos.
/// </summary>
[ApiController]
[Route("api/webhooks/mercadopago")]
public sealed class MercadoPagoWebhookController : ControllerBase
{
    private static readonly TimeSpan VentanaTsFirma = TimeSpan.FromMinutes(10);

    private readonly IPedidoService _pedidos;
    private readonly INegocioService _negocios;
    private readonly MercadoPagoOptions _opciones;
    private readonly ILogger<MercadoPagoWebhookController> _log;

    public MercadoPagoWebhookController(
        IPedidoService pedidos,
        INegocioService negocios,
        IOptions<MercadoPagoOptions> opciones,
        ILogger<MercadoPagoWebhookController> log)
    {
        _pedidos = pedidos;
        _negocios = negocios;
        _opciones = opciones.Value;
        _log = log;
    }

    [HttpPost]
    [HttpGet]
    [AllowAnonymous]
    public Task<IActionResult> Notificacion(CancellationToken ct) => ProcesarNotificacionAsync(ct);

    private async Task<IActionResult> ProcesarNotificacionAsync(CancellationToken ct)
    {
        var negocioId = Request.Query["n"].FirstOrDefault()?.Trim();

        var accessToken = _opciones.AccessToken?.Trim() ?? "";
        var webhookSecret = string.IsNullOrWhiteSpace(_opciones.WebhookSecret)
            ? null
            : _opciones.WebhookSecret.Trim();

        if (!string.IsNullOrWhiteSpace(negocioId))
        {
            var neg = await _negocios.ObtenerPorIdAsync(negocioId, ct);
            if (neg != null)
            {
                if (!string.IsNullOrWhiteSpace(neg.MercadoPagoAccessToken))
                    accessToken = neg.MercadoPagoAccessToken.Trim();
                if (!string.IsNullOrWhiteSpace(neg.MercadoPagoWebhookSecret))
                    webhookSecret = neg.MercadoPagoWebhookSecret.Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _log.LogWarning("MercadoPago webhook recibido sin AccessToken configurado; se ignora.");
            return Ok();
        }

        var notificacionOpt = await TryParseNotificacionMercadoPagoAsync(ct);
        if (notificacionOpt is null)
            return Ok();
        var notificacion = notificacionOpt.Value;

        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            var xSig = Request.Headers["x-signature"].FirstOrDefault();
            var xReq = Request.Headers["x-request-id"].FirstOrDefault();
            if (!MercadoPagoWebhookSignatureValidator.TryValidate(
                    xSig,
                    xReq,
                    notificacion.DataIdParaManifest,
                    webhookSecret,
                    VentanaTsFirma,
                    out var motivoFirma))
            {
                _log.LogWarning("MercadoPago webhook rechazado por firma: {Motivo}", motivoFirma);
                return Unauthorized();
            }
        }

        var opts = new RequestOptions { AccessToken = accessToken };

        try
        {
            return notificacion.Kind switch
            {
                MpWebhookKind.Payment => await ProcesarPagoPorIdAsync(notificacion.ResourceId, opts, ct),
                MpWebhookKind.MerchantOrder => await ProcesarOrdenComercioAsync(notificacion.ResourceId, opts, ct),
                _ => Ok(),
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error procesando webhook MP recurso {Kind} id {Id}", notificacion.Kind, notificacion.ResourceId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<IActionResult> ProcesarOrdenComercioAsync(long merchantOrderId, RequestOptions opts, CancellationToken ct)
    {
        var clienteOrden = new MerchantOrderClient();
        MerchantOrder orden = await clienteOrden.GetAsync(merchantOrderId, opts, ct);

        if (orden.Payments is null || orden.Payments.Count == 0)
        {
            _log.LogInformation("Merchant order {MerchantOrderId} sin pagos; se ignora.", merchantOrderId);
            return Ok();
        }

        var clientePago = new PaymentClient();
        foreach (var refPago in orden.Payments)
        {
            if (refPago?.Id is null)
                continue;

            var pago = await clientePago.GetAsync(refPago.Id.Value, opts, ct);
            _ = await ProcesarPagoSegunClasificacionAsync(pago, ct);
        }

        return Ok();
    }

    private async Task<IActionResult> ProcesarPagoPorIdAsync(long paymentId, RequestOptions opts, CancellationToken ct)
    {
        var cliente = new PaymentClient();
        Payment pago = await cliente.GetAsync(paymentId, opts, ct);
        return await ProcesarPagoSegunClasificacionAsync(pago, ct);
    }

    private async Task<IActionResult> ProcesarPagoSegunClasificacionAsync(Payment pago, CancellationToken ct)
    {
        var pedidoId = pago.ExternalReference?.Trim();
        var accion = MercadoPagoPaymentWebhookClassifier.Clasificar(pago.Status);

        switch (accion)
        {
            case MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Aprobado:
                if (string.IsNullOrEmpty(pedidoId))
                {
                    _log.LogWarning("Pago MP {PaymentId} aprobado sin ExternalReference.", pago.Id);
                    return Ok();
                }

                await _pedidos.ProcesarPagoAprobadoMercadoPagoAsync(pedidoId, pago.Id.ToString()!, ct);
                return Ok();

            case MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal:
                if (string.IsNullOrEmpty(pedidoId))
                    return Ok();
                await _pedidos.MarcarPedidoRechazadoSiPendienteMercadoPagoAsync(
                    pedidoId,
                    pago.Id.ToString()!,
                    pago.StatusDetail,
                    ct);
                return Ok();

            default:
                return Ok();
        }
    }

    private enum MpWebhookKind
    {
        Payment = 1,
        MerchantOrder = 2,
    }

    private readonly record struct MpWebhookParsed(MpWebhookKind Kind, long ResourceId, string DataIdParaManifest);

    private async Task<MpWebhookParsed?> TryParseNotificacionMercadoPagoAsync(CancellationToken ct)
    {
        var dataIdQuery = Request.Query["data.id"].FirstOrDefault();
        var topic = Request.Query["topic"].FirstOrDefault();
        var idLegacy = Request.Query["id"].FirstOrDefault();

        if (string.Equals(topic, "payment", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(idLegacy, out var idIpnPago))
        {
            var manifestId = !string.IsNullOrWhiteSpace(dataIdQuery) ? dataIdQuery : idLegacy;
            return new MpWebhookParsed(MpWebhookKind.Payment, idIpnPago, manifestId ?? "");
        }

        if (string.Equals(topic, "merchant_order", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(idLegacy, out var idIpnOrden))
        {
            var manifestId = !string.IsNullOrWhiteSpace(dataIdQuery) ? dataIdQuery : idLegacy;
            return new MpWebhookParsed(MpWebhookKind.MerchantOrder, idIpnOrden, manifestId ?? "");
        }

        if (!string.Equals(topic, "merchant_order", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(dataIdQuery, out var idSoloQuery))
        {
            return new MpWebhookParsed(MpWebhookKind.Payment, idSoloQuery, dataIdQuery ?? "");
        }

        if (!HttpMethods.IsPost(Request.Method))
            return null;

        var ctHeader = Request.ContentType;
        if (ctHeader is null || !ctHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return null;

        Request.EnableBuffering();
        Request.Body.Position = 0;
        try
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            Request.Body.Position = 0;

            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl))
                return null;
            var type = typeEl.GetString();
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idEl))
                return null;

            if (string.Equals(type, "payment", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDataIdLong(idEl, out var idPago, out var dataIdBody))
                    return null;
                var manifestId = !string.IsNullOrWhiteSpace(dataIdQuery) ? dataIdQuery : (dataIdBody ?? idPago.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return new MpWebhookParsed(MpWebhookKind.Payment, idPago, manifestId);
            }

            if (string.Equals(type, "merchant_order", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDataIdLong(idEl, out var idOrden, out var dataIdBodyMo))
                    return null;
                var manifestIdMo = !string.IsNullOrWhiteSpace(dataIdQuery) ? dataIdQuery : (dataIdBodyMo ?? idOrden.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return new MpWebhookParsed(MpWebhookKind.MerchantOrder, idOrden, manifestIdMo);
            }
        }
        catch (JsonException)
        {
            Request.Body.Position = 0;
        }

        return null;
    }

    private static bool TryReadDataIdLong(JsonElement idEl, out long id, out string? dataIdBody)
    {
        id = 0;
        dataIdBody = null;
        if (idEl.ValueKind == JsonValueKind.String)
        {
            dataIdBody = idEl.GetString();
            return long.TryParse(dataIdBody, out id);
        }

        if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var idNum))
        {
            id = idNum;
            dataIdBody = idNum.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }
}
