using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Authorization;

/// <summary>
/// Valida aislamiento de tenant: JWT <c>negocio_id</c> = negocio del <c>slug</c> en ruta (SuperAdmin exceptuado).
/// </summary>
public sealed class RequireMatchingNegocioAttribute : TypeFilterAttribute
{
    public RequireMatchingNegocioAttribute(string slugRouteKey = "slug")
        : base(typeof(RequireMatchingNegocioFilter))
    {
        Arguments = new object[] { slugRouteKey };
    }
}
