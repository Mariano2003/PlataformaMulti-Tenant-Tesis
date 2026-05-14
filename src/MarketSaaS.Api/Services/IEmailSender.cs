namespace MarketSaaS.Api.Services;

public interface IEmailSender
{
    Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoPlano, CancellationToken ct = default);
}
