namespace MarketSaaS.Api.Authorization;

public static class Policies
{
    public const string SuperAdminOnly = nameof(SuperAdminOnly);
    public const string AdminTiendaOnly = nameof(AdminTiendaOnly);
    public const string ClienteOnly = nameof(ClienteOnly);
    public const string SuperAdminOrAdminTienda = nameof(SuperAdminOrAdminTienda);
}
