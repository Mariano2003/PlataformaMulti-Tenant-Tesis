namespace MarketSaaS.Api.DTOs;

/// <summary>Respuesta paginada (query <c>pagina</c> 1-based, <c>tamano</c>).</summary>
public sealed class PaginaResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Pagina { get; set; }
    public int Tamano { get; set; }
    public long Total { get; set; }
    public int TotalPaginas { get; set; }
}
