namespace MarketSaaS.Api.Models;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string AdminTienda = "AdminTienda";
    public const string Cliente = "Cliente";

    public static bool IsValid(string rol) =>
        rol is SuperAdmin or AdminTienda or Cliente;
}
