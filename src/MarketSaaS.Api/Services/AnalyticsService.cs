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
            estado == PedidoEstados.Pagado || estado == PedidoEstados.Confirmado;

        static DateTime DiaUtc(DateTime t)
        {
            var utc = t.Kind == DateTimeKind.Utc ? t : DateTime.SpecifyKind(t, DateTimeKind.Utc);
            return utc.Date;
        }

        var pagadosVentana = todos.Where(p => EsVentaContada(p.Estado) && DiaUtc(p.CreadoEn) >= inicio && DiaUtc(p.CreadoEn) <= fin).ToList();

        var ventasPorDia = dias.Select(d => new VentaPorDiaDto
        {
            Fecha = d.ToString("yyyy-MM-dd"),
            CantidadPedidos = pagadosVentana.Count(p => DiaUtc(p.CreadoEn) == d),
            MontoTotal = pagadosVentana.Where(p => DiaUtc(p.CreadoEn) == d).Sum(p => p.Total),
        }).ToList();

        return new VentasResumenResponse
        {
            PedidosPorEstado = pedidosPorEstado,
            MontoTotalVentana = pagadosVentana.Sum(p => p.Total),
            PedidosPagadosVentana = pagadosVentana.Count,
            VentasPorDia = ventasPorDia,
        };
    }
}
