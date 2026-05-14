using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketSaaS.Api.Authorization;

/// <summary>
/// Exige que el negocio identificado por el slug en la ruta
/// coincida con el claim <c>negocio_id</c> del JWT. SuperAdmin queda exento (acceso a cualquier slug existente).
/// </summary>
public sealed class RequireMatchingNegocioFilter : IAsyncAuthorizationFilter
{
    private readonly INegocioService _negocios;
    private readonly string _slugRouteKey;

    public RequireMatchingNegocioFilter(INegocioService negocios, string slugRouteKey = "slug")
    {
        _negocios = negocios;
        _slugRouteKey = string.IsNullOrWhiteSpace(slugRouteKey) ? "slug" : slugRouteKey;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.IsInRole(Roles.SuperAdmin))
        {
            await TryAttachNegocioAsync(context, requireMatch: false);
            return;
        }

        var negocioIdClaim = user.FindFirst("negocio_id")?.Value;
        if (string.IsNullOrEmpty(negocioIdClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        await TryAttachNegocioAsync(context, requireMatch: true, negocioIdClaim);
    }

    private async Task TryAttachNegocioAsync(
        AuthorizationFilterContext context,
        bool requireMatch,
        string? negocioIdClaim = null)
    {
        if (context.RouteData.Values.TryGetValue(_slugRouteKey, out var valorSegmentoRuta) is not true || valorSegmentoRuta is null)
        {
            context.Result = new BadRequestObjectResult(new { error = $"Falta el parámetro de ruta '{_slugRouteKey}'." });
            return;
        }

        var slugNegocio = valorSegmentoRuta.ToString();
        if (string.IsNullOrWhiteSpace(slugNegocio))
        {
            context.Result = new BadRequestObjectResult(new { error = "El slug del negocio es inválido." });
            return;
        }

        var negocio = await _negocios.ObtenerPorSlugAsync(slugNegocio, context.HttpContext.RequestAborted);
        if (negocio is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        if (requireMatch && negocio.Id != negocioIdClaim)
        {
            context.Result = new ForbidResult();
            return;
        }

        context.HttpContext.Items[HttpContextItemKeys.NegocioActual] = negocio;
    }
}
