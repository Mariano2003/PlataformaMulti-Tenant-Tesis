namespace MarketSaaS.Api.Services;

public interface IMercadoPagoOAuthService
{
    bool ConnectHabilitado { get; }

    Task<string> IniciarAutorizacionAsync(string negocioId, string slug, CancellationToken ct = default);

    /// <summary>Intercambia el código y guarda credenciales en el negocio. Devuelve slug para redirigir al admin.</summary>
    Task<string> CompletarAutorizacionAsync(string code, string state, CancellationToken ct = default);
}
