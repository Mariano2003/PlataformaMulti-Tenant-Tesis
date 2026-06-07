using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Infrastructure;

public static class PaginacionConsulta
{
    public const int TamanoPorDefecto = 20;
    public const int TamanoMaximo = 100;

    public static (int Pagina, int Tamano, int Skip) Normalizar(int pagina, int tamano, int tamanoMaximo = TamanoMaximo)
    {
        var p = Math.Max(1, pagina);
        var t = Math.Clamp(tamano < 1 ? TamanoPorDefecto : tamano, 1, tamanoMaximo);
        return (p, t, (p - 1) * t);
    }

    public static PaginaResponse<T> Armar<T>(IReadOnlyList<T> items, int pagina, int tamano, long total)
    {
        var totalPaginas = tamano > 0 ? (int)Math.Ceiling(total / (double)tamano) : 0;
        return new PaginaResponse<T>
        {
            Items = items,
            Pagina = pagina,
            Tamano = tamano,
            Total = total,
            TotalPaginas = totalPaginas,
        };
    }
}
