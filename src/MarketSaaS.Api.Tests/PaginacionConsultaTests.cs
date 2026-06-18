using MarketSaaS.Api.Infrastructure;

namespace MarketSaaS.Api.Tests;

public sealed class PaginacionConsultaTests
{
    [Theory]
    [InlineData(0, 0, 1, 20, 0)]
    [InlineData(1, 10, 1, 10, 0)]
    [InlineData(3, 15, 3, 15, 30)]
    [InlineData(-5, 500, 1, 100, 0)]
    public void Normalizar_clampa_pagina_y_tamano(int pagina, int tamano, int expPagina, int expTamano, int expSkip)
    {
        var (p, t, skip) = PaginacionConsulta.Normalizar(pagina, tamano);

        Assert.Equal(expPagina, p);
        Assert.Equal(expTamano, t);
        Assert.Equal(expSkip, skip);
    }

    [Fact]
    public void Armar_calcula_total_paginas()
    {
        var resp = PaginacionConsulta.Armar(new[] { "a", "b" }, pagina: 2, tamano: 10, total: 25);

        Assert.Equal(2, resp.Pagina);
        Assert.Equal(10, resp.Tamano);
        Assert.Equal(25, resp.Total);
        Assert.Equal(3, resp.TotalPaginas);
        Assert.Equal(2, resp.Items.Count);
    }

    [Fact]
    public void Armar_total_cero_sin_paginas()
    {
        var resp = PaginacionConsulta.Armar(Array.Empty<string>(), pagina: 1, tamano: 20, total: 0);

        Assert.Equal(0, resp.TotalPaginas);
        Assert.Empty(resp.Items);
    }
}
