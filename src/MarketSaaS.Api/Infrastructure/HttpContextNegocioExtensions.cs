using MarketSaaS.Api.Models;
using Microsoft.AspNetCore.Http;

namespace MarketSaaS.Api.Infrastructure;

/// <summary>
/// Lectura del negocio actual establecido por <c>[RequireMatchingNegocio]</c> en <see cref="HttpContext.Items"/>.
/// </summary>
public static class HttpContextNegocioExtensions
{
    /// <summary>Indica si el filtro de tenant ya dejó el <see cref="Negocio"/> en contexto.</summary>
    public static bool TryGetNegocioActual(this HttpContext httpContext, out Negocio negocio)
    {
        if (httpContext.Items.TryGetValue(HttpContextItemKeys.NegocioActual, out var valor)
            && valor is Negocio negocioDesdeFiltroTenant)
        {
            negocio = negocioDesdeFiltroTenant;
            return true;
        }

        negocio = null!;
        return false;
    }
}
