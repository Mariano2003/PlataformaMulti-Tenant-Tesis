using MarketSaaS.Api.Infrastructure;

namespace MarketSaaS.Api.Tests;

public sealed class FrontAppUrlsTests
{
    [Theory]
    [InlineData("http://localhost:5173", "")]
    [InlineData("http://127.0.0.1:5173", "")]
    [InlineData("https://marketsaas-web.onrender.com", "/#")]
    public void PrefijoRutaSpa_localhost_sin_hash(string appBase, string prefijoEsperado)
    {
        Assert.Equal(prefijoEsperado, FrontAppUrls.PrefijoRutaSpa(appBase));
    }

    [Fact]
    public void Construir_produccion_usa_hash_router()
    {
        var url = FrontAppUrls.Construir(
            "https://marketsaas-web.onrender.com",
            "/mis-pedidos");

        Assert.Equal("https://marketsaas-web.onrender.com/#/mis-pedidos", url);
    }

    [Fact]
    public void Construir_localhost_sin_hash()
    {
        var url = FrontAppUrls.Construir("http://localhost:5173", "/restablecer-clave/abc123");

        Assert.Equal("http://localhost:5173/restablecer-clave/abc123", url);
    }

    [Theory]
    [InlineData("https://app.com/#", "https://app.com")]
    [InlineData("  https://app.com/  ", "https://app.com")]
    public void NormalizarBase_quita_hash_y_espacios(string input, string esperado)
    {
        Assert.Equal(esperado, FrontAppUrls.NormalizarBase(input));
    }
}
