using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public class ChatRoomService : IChatRoomService
{
    private const int MaxHistorial = 100;
    private readonly IMongoCollection<ChatMensaje> _mensajes;

    public ChatRoomService(IMongoDatabase db) =>
        _mensajes = db.GetCollection<ChatMensaje>(CollectionNames.ChatMensajes);

    public async Task<IReadOnlyList<ChatMensajeDto>> ListarHistorialAsync(string negocioId, CancellationToken ct = default)
    {
        var ultimos = await _mensajes
            .Find(m => m.NegocioId == negocioId)
            .SortByDescending(m => m.EnviadoEn)
            .Limit(MaxHistorial)
            .ToListAsync(ct);

        ultimos.Reverse();
        return ultimos.Select(Map).ToList();
    }

    public async Task<ChatMensajeDto> AgregarAsync(
        string negocioId,
        string slugNorm,
        string remitenteTipo,
        string remitenteNombre,
        string texto,
        CancellationToken ct = default)
    {
        var doc = new ChatMensaje
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NegocioId = negocioId,
            Slug = slugNorm,
            RemitenteTipo = remitenteTipo,
            RemitenteNombre = remitenteNombre,
            Texto = texto,
            EnviadoEn = DateTime.UtcNow,
        };

        await _mensajes.InsertOneAsync(doc, cancellationToken: ct);
        return Map(doc);
    }

    private static ChatMensajeDto Map(ChatMensaje m) => new()
    {
        Id = m.Id,
        Slug = m.Slug,
        RemitenteTipo = m.RemitenteTipo,
        RemitenteNombre = m.RemitenteNombre,
        Texto = m.Texto,
        EnviadoEn = m.EnviadoEn,
    };
}