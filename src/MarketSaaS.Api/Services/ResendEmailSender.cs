using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

/// <summary>Envío por HTTPS (puerto 443): funciona en Render free donde SMTP 587 está bloqueado.</summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ResendEmailSender> _log;

    public ResendEmailSender(
        IOptions<EmailOptions> opt,
        IHttpClientFactory httpFactory,
        ILogger<ResendEmailSender> log)
    {
        _opt = opt.Value;
        _httpFactory = httpFactory;
        _log = log;
    }

    public async Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoPlano, CancellationToken ct = default)
    {
        if (!_opt.Enabled)
        {
            _log.LogWarning("Email:Enabled=false — no se envía correo a {Destinatario}", destinatarioEmail);
            return;
        }

        var apiKey = _opt.ResendApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Email:ResendApiKey está vacío.");

        var from = ResolverRemitente();
        var cliente = _httpFactory.CreateClient(nameof(ResendEmailSender));
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var payload = JsonSerializer.Serialize(new
        {
            from,
            to = new[] { destinatarioEmail },
            subject = asunto,
            text = cuerpoPlano,
        });
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var res = await cliente.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            _log.LogError(
                "Resend falló ({Status}) para {Destinatario}: {Body}",
                (int)res.StatusCode,
                destinatarioEmail,
                body);
            throw new InvalidOperationException(
                $"No se pudo enviar el correo (Resend {(int)res.StatusCode}). Revisá remitente verificado en resend.com.");
        }

        _log.LogInformation("Correo enviado vía Resend a {Destinatario} (asunto: {Asunto}).", destinatarioEmail, asunto);
    }

    private string ResolverRemitente()
    {
        if (!string.IsNullOrWhiteSpace(_opt.ResendFrom))
            return _opt.ResendFrom.Trim();

        if (string.IsNullOrWhiteSpace(_opt.FromEmail))
            throw new InvalidOperationException(
                "Configurá Email:FromEmail o Email:ResendFrom (email verificado en Resend).");

        var nombre = string.IsNullOrWhiteSpace(_opt.FromName) ? "MarketSaaS" : _opt.FromName.Trim();
        return $"{nombre} <{_opt.FromEmail.Trim()}>";
    }
}
