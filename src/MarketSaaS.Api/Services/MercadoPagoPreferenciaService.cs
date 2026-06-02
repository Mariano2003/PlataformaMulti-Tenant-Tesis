using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class MercadoPagoPreferenciaService : IMercadoPagoPreferenciaService
{
    private readonly INegocioService _negocios;
    private readonly IPedidoService _pedidos;
    private readonly IMercadoPagoAccessTokenProvider _accessTokens;
    private readonly MercadoPagoOptions _opciones;
    private readonly EmailOptions _email;
    private readonly ILogger<MercadoPagoPreferenciaService> _log;

    public MercadoPagoPreferenciaService(
        INegocioService negocios,
        IPedidoService pedidos,
        IMercadoPagoAccessTokenProvider accessTokens,
        IOptions<MercadoPagoOptions> opciones,
        IOptions<EmailOptions> email,
        ILogger<MercadoPagoPreferenciaService> log)
    {
        _negocios = negocios;
        _pedidos = pedidos;
        _accessTokens = accessTokens;
        _opciones = opciones.Value;
        _email = email.Value;
        _log = log;
    }

    public async Task<PreferenciaMercadoPagoResponse> CrearPreferenciaCheckoutProAsync(
        string slugNegocio,
        string pedidoId,
        CancellationToken ct = default)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slugNegocio, ct)
            ?? throw new InvalidOperationException("El negocio no existe.");

        var accessToken = await _accessTokens.ObtenerParaNegocioAsync(negocio, ct);

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Mercado Pago: conectá tu cuenta en el panel de la tienda (OAuth o token manual) o configurá MercadoPago:AccessToken en la API.");

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

        var appBase = ObtenerUrlBaseFront();
        ValidarUrlBaseFront(appBase, baseUrl);
        var backUrlSuccess = ResolverUrlRetornoTienda(_opciones.BackUrlSuccess, appBase, slugNegocio, "ok");
        var backUrlFailure = ResolverUrlRetornoTienda(_opciones.BackUrlFailure, appBase, slugNegocio, "error");
        var backUrlPending = ResolverUrlRetornoTienda(_opciones.BackUrlPending, appBase, slugNegocio, "pending");

        _log.LogInformation(
            "MP preferencia pedido {PedidoId} tienda {Slug}: retorno success={Success}",
            pedidoId,
            slugNegocio,
            backUrlSuccess);

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
            UrlRetornoExito = backUrlSuccess,
        };
    }

    private void ValidarUrlBaseFront(string appBase, string apiBase)
    {
        if (string.Equals(appBase, apiBase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "MercadoPago:PublicAppBaseUrl no puede ser la misma URL que PublicApiBaseUrl. " +
                "PublicAppBaseUrl debe ser la del front (static site), ej. https://tu-app.onrender.com");

        if (appBase.Contains("/api", StringComparison.OrdinalIgnoreCase) ||
            appBase.Contains("swagger", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "MercadoPago:PublicAppBaseUrl parece apuntar a la API. Usá la URL del sitio Vue (static site).");

        if (!Uri.TryCreate(appBase, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException(
                "MercadoPago:PublicAppBaseUrl debe ser una URL absoluta http(s) del front.");
    }

    private string ObtenerUrlBaseFront()
    {
        var app = _opciones.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(app))
            return app;
        app = _email.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(app))
            return app;
        throw new InvalidOperationException(
            "Configurá MercadoPago:PublicAppBaseUrl o Email:PublicAppBaseUrl con la URL pública del front (ej. https://tu-app.onrender.com).");
    }

    private static string ResolverUrlRetornoTienda(string? plantilla, string appBase, string slug, string pago)
    {
        if (!string.IsNullOrWhiteSpace(plantilla) && plantilla.Contains("{slug}", StringComparison.Ordinal))
            return plantilla.Replace("{slug}", Uri.EscapeDataString(slug.Trim()), StringComparison.Ordinal);

        var slugPath = Uri.EscapeDataString(slug.Trim());
        return FrontAppUrls.Construir(appBase, $"/tienda/{slugPath}?pago={pago}");
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
