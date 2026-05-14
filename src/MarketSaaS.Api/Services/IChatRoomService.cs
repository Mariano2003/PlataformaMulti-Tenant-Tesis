using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

public interface IChatRoomService
{
    Task<IReadOnlyList<ChatMensajeDto>> ListarHistorialAsync(string negocioId, CancellationToken ct = default);

    Task<ChatMensajeDto> AgregarAsync(
        string negocioId,
        string slugNorm,
        string remitenteTipo,
        string remitenteNombre,
        string texto,
        CancellationToken ct = default);
}