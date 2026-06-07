using System.Security.Claims;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/mis-pedidos")]
[Authorize]
public sealed class MisPedidosController : ControllerBase
{
    private readonly IPedidoService _pedidos;
    private readonly INegocioService _negocios;

    public MisPedidosController(IPedidoService pedidos, INegocioService negocios)
    {
        _pedidos = pedidos;
        _negocios = negocios;
    }

    /// <summary>Pedidos del usuario logueado (por email en JWT), más recientes primero.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PedidoClienteListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PedidoClienteListItemResponse>>> Listar(
        [FromQuery] int limite = 50,
        CancellationToken ct = default)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(usuarioId))
            return Unauthorized();

        var pedidos = await _pedidos.ListarPorClienteAsync(email, usuarioId, limite, ct);
        if (pedidos.Count == 0)
            return Ok(Array.Empty<PedidoClienteListItemResponse>());

        var negocioIds = pedidos.Select(p => p.NegocioId).Distinct().ToList();
        var negociosPorId = new Dictionary<string, Negocio>(StringComparer.Ordinal);
        foreach (var id in negocioIds)
        {
            var neg = await _negocios.ObtenerPorIdAsync(id, ct);
            if (neg != null)
                negociosPorId[id] = neg;
        }

        var respuesta = pedidos.Select(p =>
        {
            negociosPorId.TryGetValue(p.NegocioId, out var neg);
            return MapCliente(p, neg);
        }).ToList();

        return Ok(respuesta);
    }

    private static PedidoClienteListItemResponse MapCliente(Pedido pedido, Negocio? negocio) => new()
    {
        Id = pedido.Id,
        NegocioId = pedido.NegocioId,
        NegocioSlug = negocio?.Slug ?? "",
        NegocioNombre = negocio?.Nombre ?? "Tienda",
        Estado = pedido.Estado,
        Total = pedido.Total,
        CreadoEn = pedido.CreadoEn,
        Lineas = pedido.Lineas.Select(linea => new PedidoLineaResponse
        {
            ProductoId = linea.ProductoId,
            Nombre = linea.Nombre,
            Cantidad = linea.Cantidad,
            PrecioUnitario = linea.PrecioUnitario,
            Subtotal = linea.Subtotal,
        }).ToList(),
    };
}
