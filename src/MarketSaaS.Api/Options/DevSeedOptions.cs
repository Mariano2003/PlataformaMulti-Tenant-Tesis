namespace MarketSaaS.Api.Options;

/// <summary>
/// Semilla opcional en desarrollo: usuarios con cada rol y tienda demo.
/// Los valores por defecto coinciden con <c>MarketSaaS.Api.http</c>.
/// </summary>
public sealed class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    /// <summary>Solo tiene efecto si la aplicación está en entorno Development.</summary>
    public bool Enabled { get; set; }

    public string SuperAdminEmail { get; set; } = "admin@plataforma.com";
    public string SuperAdminPassword { get; set; } = "ClaveSegura1";

    /// <summary>Crea negocio demo + AdminTienda + Cliente sin negocio.</summary>
    public bool SeedDemoTienda { get; set; } = true;

    public string DemoNegocioSlug { get; set; } = "mi-tienda-demo";
    public string DemoNegocioNombre { get; set; } = "Mi Tienda Demo";

    public string AdminTiendaEmail { get; set; } = "dueño@mitienda.com";
    public string AdminTiendaPassword { get; set; } = "ClaveSegura1";

    public string ClienteEmail { get; set; } = "cliente@ejemplo.com";
    public string ClientePassword { get; set; } = "ClaveSegura1";
}
