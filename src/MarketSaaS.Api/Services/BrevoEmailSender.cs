using System.Text;
using System.Text.Json;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

/// <summary>
/// Envío vía API HTTP de Brevo (puerto 443: funciona en Render free).
/// Permite enviar a cualquier destinatario sin dominio propio: solo hay que
/// verificar el email remitente en brevo.com → Senders.
/// </summary>
public sealed class BrevoEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<BrevoEmailSender> _log;

    public BrevoEmailSender(
        IOptions<EmailOptions> opt,
        IHttpClientFactory httpFactory,
        ILogger<BrevoEmailSender> log)
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

        var apiKey = _opt.BrevoApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Email:BrevoApiKey está vacío.");

        var fromEmail = _opt.FromEmail?.Trim();
        if (string.IsNullOrEmpty(fromEmail))
            throw new InvalidOperationException(
                "Configurá Email:FromEmail con el remitente verificado en Brevo (Senders).");

        var fromName = string.IsNullOrWhiteSpace(_opt.FromName) ? "MarketSaaS" : _opt.FromName.Trim();

        var cliente = _httpFactory.CreateClient(nameof(BrevoEmailSender));
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        req.Headers.Add("api-key", apiKey);
        var payload = JsonSerializer.Serialize(new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new[] { new { email = destinatarioEmail } },
            subject = asunto,
            textContent = cuerpoPlano,
        });
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var res = await cliente.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            _log.LogError(
                "Brevo falló ({Status}) para {Destinatario}: {Body}",
                (int)res.StatusCode,
                destinatarioEmail,
                body);
            throw new InvalidOperationException(
                $"No se pudo enviar el correo (Brevo {(int)res.StatusCode}). Revisá que Email:FromEmail sea un remitente verificado en brevo.com → Senders.");
        }

        _log.LogInformation("Correo enviado vía Brevo a {Destinatario} (asunto: {Asunto}).", destinatarioEmail, asunto);
    }
}
