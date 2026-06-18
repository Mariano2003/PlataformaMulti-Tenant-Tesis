using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Tests;

public sealed class PedidoEstadosTests
{
    [Theory]
    [InlineData(PedidoEstados.Pagado, true)]
    [InlineData(PedidoEstados.Confirmado, true)]
    [InlineData(PedidoEstados.EnPreparacion, true)]
    [InlineData(PedidoEstados.Enviado, true)]
    [InlineData(PedidoEstados.PendientePago, false)]
    [InlineData(PedidoEstados.ProcesandoPago, false)]
    [InlineData(PedidoEstados.Rechazado, false)]
    [InlineData(PedidoEstados.Entregado, false)]
    [InlineData(PedidoEstados.Cancelado, false)]
    public void AdminPuedeGestionar_solo_post_pago_en_curso(string estado, bool esperado)
    {
        Assert.Equal(esperado, PedidoEstados.AdminPuedeGestionar(estado));
    }

    [Fact]
    public void EstadosGestionAdmin_incluye_flujo_logistico()
    {
        Assert.Contains(PedidoEstados.EnPreparacion, PedidoEstados.EstadosGestionAdmin);
        Assert.Contains(PedidoEstados.Enviado, PedidoEstados.EstadosGestionAdmin);
        Assert.Contains(PedidoEstados.Entregado, PedidoEstados.EstadosGestionAdmin);
        Assert.Contains(PedidoEstados.Cancelado, PedidoEstados.EstadosGestionAdmin);
        Assert.DoesNotContain(PedidoEstados.Pagado, PedidoEstados.EstadosGestionAdmin);
    }
}
