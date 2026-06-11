using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

/// <summary>Correos al cliente sobre su pedido. Nunca lanza: si el envío falla solo se loguea.</summary>
public interface IPedidoEmailService
{
    /// <summary>Confirmación de compra cuando el pago queda aprobado (estado <see cref="PedidoEstados.Pagado"/>).</summary>
    Task NotificarPagoConfirmadoAsync(Pedido pedido, CancellationToken ct = default);

    /// <summary>Aviso cuando el admin cambia el estado (en preparación, enviado, entregado, cancelado).</summary>
    Task NotificarCambioEstadoAsync(Pedido pedido, CancellationToken ct = default);
}
