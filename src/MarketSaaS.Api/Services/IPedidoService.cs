using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface IPedidoService
{
    /// <summary>Crea pedido en <see cref="PedidoEstados.PendientePago"/> sin descontar stock (Checkout Pro + webhook).</summary>
    Task<Pedido> CrearPendienteDePagoAsync(string negocioId, CrearPedidoRequest solicitud, CancellationToken ct = default);

    /// <summary>Guarda el id de preferencia MP en el pedido pendiente.</summary>
    Task<bool> AsociarPreferenciaMercadoPagoAsync(string negocioId, string pedidoId, string preferenceId, CancellationToken ct = default);

    /// <summary>Idempotente: si el pago está aprobado y el pedido sigue pendiente, descuenta stock y marca <see cref="PedidoEstados.Pagado"/>.</summary>
    Task ProcesarPagoAprobadoMercadoPagoAsync(string pedidoId, string mercadoPagoPaymentId, CancellationToken ct = default);

    /// <summary>Si el pedido sigue en <see cref="PedidoEstados.PendientePago"/>, lo pasa a <see cref="PedidoEstados.Rechazado"/> y guarda id y detalle del pago MP (idempotente).</summary>
    Task MarcarPedidoRechazadoSiPendienteMercadoPagoAsync(
        string pedidoId,
        string mercadoPagoPaymentId,
        string? mercadoPagoStatusDetail = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Pedido>> ListarPorNegocioAsync(string negocioId, int limite, CancellationToken ct = default);

    /// <summary>Pedidos cuyo <see cref="Pedido.ClienteEmail"/> coincide (comparación sin distinguir mayúsculas).</summary>
    Task<IReadOnlyList<Pedido>> ListarPorClienteEmailAsync(string clienteEmail, int limite, CancellationToken ct = default);

    Task<Pedido?> ObtenerPorIdYNegocioAsync(string id, string negocioId, CancellationToken ct = default);
}
