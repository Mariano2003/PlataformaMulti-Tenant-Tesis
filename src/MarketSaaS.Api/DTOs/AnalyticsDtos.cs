namespace MarketSaaS.Api.DTOs;

public class PedidoEstadoConteoDto
{
    public string Estado { get; set; } = null!;
    public int Cantidad { get; set; }
}

public class VentaPorDiaDto
{
    /// <summary>Fecha en UTC como <c>yyyy-MM-dd</c>.</summary>
    public string Fecha { get; set; } = null!;

    public int CantidadPedidos { get; set; }

    public decimal MontoTotal { get; set; }
}

public class VentasResumenResponse
{
    /// <summary>Conteo por estado (todos los pedidos del negocio).</summary>
    public IReadOnlyList<PedidoEstadoConteoDto> PedidosPorEstado { get; set; } = [];

    /// <summary>Suma de totales de pedidos pagados en los últimos 30 días (UTC).</summary>
    public decimal MontoTotalVentana { get; set; }

    public int PedidosPagadosVentana { get; set; }

    /// <summary>Serie diaria de los últimos 30 días (UTC); solo pedidos pagados/confirmados.</summary>
    public IReadOnlyList<VentaPorDiaDto> VentasPorDia { get; set; } = [];
}
