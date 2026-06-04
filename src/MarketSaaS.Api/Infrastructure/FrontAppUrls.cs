namespace MarketSaaS.Api.Infrastructure;

/// <summary>Rutas del SPA Vue: en hosting estático (Render) se usa prefijo <c>/#</c> para evitar 404.</summary>
public static class FrontAppUrls
{
    /// <summary>Vacío en localhost; <c>/#</c> en URLs públicas (alineado con el router en producción).</summary>
    public static string PrefijoRutaSpa(string appBase)
    {
        if (!Uri.TryCreate(appBase, UriKind.Absolute, out var uri))
            return "/#";

        var host = uri.Host.ToLowerInvariant();
        return host is "localhost" or "127.0.0.1" or "::1" ? "" : "/#";
    }

    public static string Construir(string appBase, string rutaYQuery, bool usarHashRouter = true)
    {
        var baseNorm = NormalizarBase(appBase);
        var ruta = rutaYQuery.StartsWith('/') ? rutaYQuery : "/" + rutaYQuery;
        var prefijo = usarHashRouter ? PrefijoRutaSpa(baseNorm) : "";
        return $"{baseNorm}{prefijo}{ruta}";
    }

    /// <summary>Quita <c>/#</c> final por si la URL del front se copió con hash.</summary>
    public static string NormalizarBase(string appBase)
    {
        var baseNorm = appBase.Trim().TrimEnd('/');
        if (baseNorm.EndsWith("/#", StringComparison.OrdinalIgnoreCase))
            baseNorm = baseNorm[..^2];
        return baseNorm;
    }
}
