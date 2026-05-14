using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class MercadoPagoPreferenciaService : IMercadoPagoPreferenciaService
{
    private readonly INegocioService _negocios;
    private readonly IPedidoService _pedidos;
    private readonly MercadoPagoOptions _opciones;

    public MercadoPagoPreferenciaService(
        INegocioService negocios,
        IPedidoService pedidos,
        IOptions<MercadoPagoOptions> opciones)
    {
        _negocios = negocios;
        _pedidos = pedidos;
        _opciones = opciones.Value;
    }

    public async Task<PreferenciaMercadoPagoResponse> CrearPreferenciaCheckoutProAsync(
        string slugNegocio,
        string pedidoId,
        CancellationToken ct = default)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slugNegocio, ct)
            ?? throw new InvalidOperationException("El negocio no existe.");

        var accessToken = !string.IsNullOrWhiteSpace(negocio.MercadoPagoAccessToken)
            ? negocio.MercadoPagoAccessToken.Trim()
            : _opciones.AccessToken?.Trim();

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Mercado Pago: configurá el Access Token en el panel de la tienda (admin) o MercadoPago:AccessToken en la API.");

        var pedido = await _pedidos.ObtenerPorIdYNegocioAsync(pedidoId, negocio.Id, ct)
            ?? throw new InvalidOperationException("El pedido no existe en esta tienda.");

        if (pedido.Estado != PedidoEstados.PendientePago)
            throw new InvalidOperationException("Solo se puede pagar un pedido en estado PendientePago.");

        var baseUrl = _opciones.PublicApiBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException(
                "MercadoPago:PublicApiBaseUrl debe ser la URL pública de la API (para webhooks y retornos), ej. https://tu-tunnel.ngrok-free.app");

        var items = pedido.Lineas.Select(linea => new PreferenceItemRequest
        {
            Title = linea.Nombre.Length > 127 ? linea.Nombre[..127] : linea.Nombre,
            Quantity = linea.Cantidad,
            CurrencyId = "ARS",
            UnitPrice = linea.PrecioUnitario,
        }).ToList();

        var backUrlSuccess = string.IsNullOrWhiteSpace(_opciones.BackUrlSuccess)
            ? $"{baseUrl}/swagger"
            : _opciones.BackUrlSuccess;
        var backUrlFailure = string.IsNullOrWhiteSpace(_opciones.BackUrlFailure)
            ? $"{baseUrl}/swagger"
            : _opciones.BackUrlFailure;
        var backUrlPending = string.IsNullOrWhiteSpace(_opciones.BackUrlPending)
            ? $"{baseUrl}/swagger"
            : _opciones.BackUrlPending;

        // MP rechaza `auto_return` si la URL de success no es pública (p. ej. apunta a localhost).
        var permiteAutoReturn = EsUrlPublicaParaMercadoPago(backUrlSuccess);

        var preferenciaRequest = new PreferenceRequest
        {
            Items = items,
            ExternalReference = pedido.Id,
            Payer = new PreferencePayerRequest
            {
                Email = pedido.ClienteEmail,
                Name = pedido.ClienteNombre,
            },
            NotificationUrl = $"{baseUrl}/api/webhooks/mercadopago?n={Uri.EscapeDataString(negocio.Id)}",
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = backUrlSuccess,
                Failure = backUrlFailure,
                Pending = backUrlPending,
            },
            AutoReturn = permiteAutoReturn ? "approved" : null,
        };

        var requestOptions = new RequestOptions { AccessToken = accessToken };
        var clientePreferencia = new PreferenceClient();
        Preference preferenciaCreada = await clientePreferencia.CreateAsync(preferenciaRequest, requestOptions, ct);

        var preferenciaId = preferenciaCreada.Id
            ?? throw new InvalidOperationException("Mercado Pago no devolvió Id de preferencia.");

        var asociado = await _pedidos.AsociarPreferenciaMercadoPagoAsync(negocio.Id, pedidoId, preferenciaId, ct);
        if (!asociado)
            throw new InvalidOperationException("No se pudo asociar la preferencia al pedido (estado u orden inválido).");

        return new PreferenciaMercadoPagoResponse
        {
            PedidoId = pedido.Id,
            PreferenciaId = preferenciaId,
            UrlPago = preferenciaCreada.InitPoint ?? preferenciaCreada.SandboxInitPoint
                ?? throw new InvalidOperationException("Mercado Pago no devolvió URL de pago."),
            UrlPagoSandbox = preferenciaCreada.SandboxInitPoint,
        };
    }

    private static bool EsUrlPublicaParaMercadoPago(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6) return false;
        var host = uri.Host.ToLowerInvariant();
        return host is not ("localhost" or "127.0.0.1" or "::1");
    }
}
