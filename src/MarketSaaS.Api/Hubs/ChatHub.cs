using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace MarketSaaS.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IChatRoomService _chat;
    private readonly INegocioService _negocios;

    public ChatHub(IChatRoomService chat, INegocioService negocios)
    {
        _chat = chat;
        _negocios = negocios;
    }

    public async Task Unirse(string slug)
    {
        var slugNorm = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slugNorm))
            throw new HubException("Slug inv�lido.");

        var negocio = await _negocios.ObtenerPorSlugAsync(slugNorm, Context.ConnectionAborted);
        if (negocio is null)
            throw new HubException("Negocio no encontrado.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(slugNorm));

        var historial = await _chat.ListarHistorialAsync(negocio.Id, Context.ConnectionAborted);
        await Clients.Caller.SendAsync("Historial", historial, Context.ConnectionAborted);
    }

    public async Task EnviarMensaje(string slug, ChatEnviarMensajeRequest req)
    {
        var slugNorm = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slugNorm))
            throw new HubException("Slug inv�lido.");

        var negocio = await _negocios.ObtenerPorSlugAsync(slugNorm, Context.ConnectionAborted);
        if (negocio is null)
            throw new HubException("Negocio no encontrado.");

        var texto = (req.Texto ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(texto))
            throw new HubException("Mensaje vac�o.");

        var esAdmin = Context.User?.IsInRole(Roles.AdminTienda) == true || Context.User?.IsInRole(Roles.SuperAdmin) == true;

        if (esAdmin)
        {
            var negocioId = Context.User?.FindFirst("negocio_id")?.Value;
            var superAdmin = Context.User?.IsInRole(Roles.SuperAdmin) == true;
            if (!superAdmin && negocioId != negocio.Id)
                throw new HubException("No autorizado para este negocio.");
        }

        var nombre = (req.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = esAdmin ? "Admin" : "Cliente";

        if (nombre.Length > 80)
            nombre = nombre[..80];
        if (texto.Length > 1000)
            texto = texto[..1000];

        var mensaje = await _chat.AgregarAsync(
            negocio.Id,
            slugNorm,
            esAdmin ? "admin" : "cliente",
            nombre,
            texto,
            Context.ConnectionAborted);

        await Clients.Group(Grupo(slugNorm)).SendAsync("MensajeNuevo", mensaje, Context.ConnectionAborted);
    }

    private static string Grupo(string slug) => $"chat:{slug}";
}
