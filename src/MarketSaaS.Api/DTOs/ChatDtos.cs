using System.ComponentModel.DataAnnotations;

namespace MarketSaaS.Api.DTOs;

public class ChatMensajeDto
{
    public string Id { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string RemitenteTipo { get; set; } = null!;
    public string RemitenteNombre { get; set; } = null!;
    public string Texto { get; set; } = null!;
    public DateTime EnviadoEn { get; set; }
}

public class ChatEnviarMensajeRequest
{
    [Required, MaxLength(80)]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Texto { get; set; } = null!;
}
