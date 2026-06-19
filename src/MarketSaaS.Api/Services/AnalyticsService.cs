using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IMongoCollection<Pedido> _pedidos;

    public AnalyticsService(IMongoDatabase db) =>
        _pedidos = db.GetCollection<Pedido>(CollectionNames.Pedidos);

    public async Task<VentasResumenResponse> ObtenerResumenPedidosAsync(string negocioId, CancellationToken ct = default)
    {
        var todos = await _pedidos.Find(p => p.NegocioId == negocioId).ToListAsync(ct);

        var pedidosPorEstado = todos
            .GroupBy(p => p.Estado)
            .Select(g => new PedidoEstadoConteoDto { Estado = g.Key, Cantidad = g.Count() })
            .OrderBy(x => x.Estado)
            .ToList();

        var fin = DateTime.UtcNow.Date;
        var inicio = fin.AddDays(-29);
        var dias = Enumerable.Range(0, 30).Select(i => inicio.AddDays(i)).ToList();

        static bool EsVentaContada(string estado) =>
            estado is PedidoEstados.Pagado
                or PedidoEstados.Confirmado
                or PedidoEstados.EnPreparacion
                or PedidoEstados.Enviado
                or PedidoEstados.Entregado;

        static DateTime DiaUtc(DateTime t)
        {
            var utc = t.Kind == DateTimeKind.Utc ? t : DateTime.SpecifyKind(t, DateTimeKind.Utc);
            return utc.Date;
        }

        var pagadosVentana = todos
            .Where(p => EsVentaContada(p.Estado) && DiaUtc(p.CreadoEn) >= inicio && DiaUtc(p.CreadoEn) <= fin)
            .ToList();

        var ventasPorDia = dias.Select(d => new VentaPorDiaDto
        {
            Fecha = d.ToString("yyyy-MM-dd"),
            CantidadPedidos = pagadosVentana.Count(p => DiaUtc(p.CreadoEn) == d),
            MontoTotal = pagadosVentana.Where(p => DiaUtc(p.CreadoEn) == d).Sum(p => p.Total),
        }).ToList();

        var productosTop = pagadosVentana
            .SelectMany(p => p.Lineas)
            .GroupBy(l => new { l.ProductoId, l.Nombre })
            .Select(g => new ProductoTopVentaDto
            {
                ProductoId = g.Key.ProductoId,
                Nombre = g.Key.Nombre,
                CantidadVendida = g.Sum(x => x.Cantidad),
                MontoTotal = g.Sum(x => x.Subtotal),
            })
            .OrderByDescending(x => x.MontoTotal)
            .Take(5)
            .ToList();

        var cantidadPagados = pagadosVentana.Count;
        var montoTotal = pagadosVentana.Sum(p => p.Total);
        var unidadesVendidas = pagadosVentana.SelectMany(p => p.Lineas).Sum(l => l.Cantidad);

        static bool PendienteEntrega(string estado) =>
            estado is PedidoEstados.Pagado
                or PedidoEstados.Confirmado
                or PedidoEstados.EnPreparacion
                or PedidoEstados.Enviado;

        var pedidosPorEntregar = todos.Count(p => PendienteEntrega(p.Estado));

        return new VentasResumenResponse
        {
            PedidosPorEstado = pedidosPorEstado,
            MontoTotalVentana = montoTotal,
            PedidosPagadosVentana = cantidadPagados,
            UnidadesVendidasVentana = unidadesVendidas,
            PedidosPorEntregar = pedidosPorEntregar,
            VentasPorDia = ventasPorDia,
            TicketPromedioVentana = cantidadPagados > 0 ? montoTotal / cantidadPagados : 0,
            ProductosTop = productosTop,
        };
    }
}
