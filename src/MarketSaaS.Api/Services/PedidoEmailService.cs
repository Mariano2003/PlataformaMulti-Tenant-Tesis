using System.Globalization;
using System.Text;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class PedidoEmailService : IPedidoEmailService
{
    private static readonly CultureInfo CulturaArs = CultureInfo.GetCultureInfo("es-AR");

    private readonly IEmailSender _email;
    private readonly INegocioService _negocios;
    private readonly EmailOptions _emailOpt;
    private readonly MercadoPagoOptions _mpOpt;
    private readonly ILogger<PedidoEmailService> _log;

    public PedidoEmailService(
        IEmailSender email,
        INegocioService negocios,
        IOptions<EmailOptions> emailOpt,
        IOptions<MercadoPagoOptions> mpOpt,
        ILogger<PedidoEmailService> log)
    {
        _email = email;
        _negocios = negocios;
        _emailOpt = emailOpt.Value;
        _mpOpt = mpOpt.Value;
        _log = log;
    }

    public async Task NotificarPagoConfirmadoAsync(Pedido pedido, CancellationToken ct = default)
    {
        if (!PuedeEnviar(pedido))
            return;

        try
        {
            var nombreTienda = await ObtenerNombreTiendaAsync(pedido.NegocioId, ct);
            var cuerpo = new StringBuilder()
                .AppendLine($"Hola {NombreCliente(pedido)},")
                .AppendLine()
                .AppendLine($"¡Recibimos tu pago! Tu pedido en {nombreTienda} quedó confirmado.")
                .AppendLine()
                .AppendLine("Detalle de la compra:")
                .AppendLine(ArmarDetalleLineas(pedido))
                .AppendLine($"Total: {FormatearPrecio(pedido.Total)}")
                .AppendLine()
                .AppendLine($"Podés seguir el estado de tu pedido acá:")
                .AppendLine(LinkMisPedidos())
                .AppendLine()
                .AppendLine("Gracias por tu compra.")
                .AppendLine("MarketSaaS")
                .ToString();

            await _email.EnviarAsync(
                pedido.ClienteEmail!,
                $"Compra confirmada en {nombreTienda} — MarketSaaS",
                cuerpo,
                ct);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "No se pudo enviar el mail de confirmación del pedido {PedidoId} a {Email}.",
                pedido.Id,
                pedido.ClienteEmail);
        }
    }

    public async Task NotificarCambioEstadoAsync(Pedido pedido, CancellationToken ct = default)
    {
        if (!PuedeEnviar(pedido))
            return;

        var (asuntoEstado, mensajeEstado) = TextosPorEstado(pedido.Estado);
        if (asuntoEstado is null || mensajeEstado is null)
            return;

        try
        {
            var nombreTienda = await ObtenerNombreTiendaAsync(pedido.NegocioId, ct);
            var cuerpo = new StringBuilder()
                .AppendLine($"Hola {NombreCliente(pedido)},")
                .AppendLine()
                .AppendLine($"{mensajeEstado} (pedido en {nombreTienda}).")
                .AppendLine()
                .AppendLine("Detalle:")
                .AppendLine(ArmarDetalleLineas(pedido))
                .AppendLine($"Total: {FormatearPrecio(pedido.Total)}")
                .AppendLine()
                .AppendLine("Seguilo desde Mis pedidos:")
                .AppendLine(LinkMisPedidos())
                .AppendLine()
                .AppendLine("MarketSaaS")
                .ToString();

            await _email.EnviarAsync(
                pedido.ClienteEmail!,
                $"{asuntoEstado} — {nombreTienda}",
                cuerpo,
                ct);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "No se pudo enviar el mail de estado «{Estado}» del pedido {PedidoId} a {Email}.",
                pedido.Estado,
                pedido.Id,
                pedido.ClienteEmail);
        }
    }

    private bool PuedeEnviar(Pedido pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido.ClienteEmail))
            return false;

        if (!_emailOpt.Enabled)
        {
            _log.LogDebug("Email:Enabled=false — se omite el mail del pedido {PedidoId}.", pedido.Id);
            return false;
        }

        return true;
    }

    private static (string? asunto, string? mensaje) TextosPorEstado(string estado) => estado switch
    {
        PedidoEstados.EnPreparacion => ("Tu pedido está en preparación", "La tienda ya está preparando tu pedido"),
        PedidoEstados.Enviado => ("Tu pedido está en camino", "Tu pedido salió de la tienda y está en camino"),
        PedidoEstados.Entregado => ("Tu pedido fue entregado", "Tu pedido figura como entregado. ¡Que lo disfrutes!"),
        PedidoEstados.Cancelado => ("Tu pedido fue cancelado", "La tienda canceló tu pedido. Si tenés dudas, contactala directamente"),
        _ => (null, null),
    };

    private async Task<string> ObtenerNombreTiendaAsync(string negocioId, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorIdAsync(negocioId, ct);
        return string.IsNullOrWhiteSpace(negocio?.Nombre) ? "la tienda" : negocio.Nombre;
    }

    private static string NombreCliente(Pedido pedido) =>
        string.IsNullOrWhiteSpace(pedido.ClienteNombre) ? "cliente" : pedido.ClienteNombre.Trim();

    private static string ArmarDetalleLineas(Pedido pedido)
    {
        if (pedido.Lineas.Count == 0)
            return "  (sin detalle de productos)";

        var sb = new StringBuilder();
        foreach (var linea in pedido.Lineas)
        {
            sb.AppendLine(
                $"  - {linea.Nombre} x{linea.Cantidad} ({FormatearPrecio(linea.PrecioUnitario)} c/u) = {FormatearPrecio(linea.Subtotal)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatearPrecio(decimal valor) =>
        valor.ToString("C2", CulturaArs);

    private string LinkMisPedidos()
    {
        var app = _emailOpt.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(app))
            app = _mpOpt.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(app))
            app = "http://localhost:5173";

        return FrontAppUrls.Construir(app, "/mis-pedidos");
    }
}
