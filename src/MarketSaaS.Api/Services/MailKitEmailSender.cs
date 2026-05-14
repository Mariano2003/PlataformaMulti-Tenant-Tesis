using MailKit.Net.Smtp;
using MailKit.Security;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MarketSaaS.Api.Services;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<MailKitEmailSender> _log;

    public MailKitEmailSender(IOptions<EmailOptions> opt, ILogger<MailKitEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoPlano, CancellationToken ct = default)
    {
        if (!_opt.Enabled)
        {
            _log.LogWarning("Email:Enabled=false — no se envía correo a {Destinatario}", destinatarioEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(_opt.SmtpUser) || string.IsNullOrWhiteSpace(_opt.SmtpPassword))
            throw new InvalidOperationException(
                "Email.Enabled es true pero faltan SmtpUser o SmtpPassword en la configuración.");

        if (string.IsNullOrWhiteSpace(_opt.FromEmail))
            throw new InvalidOperationException(
                "Email.Enabled es true pero FromEmail está vacío. Suele ser la misma cuenta que SmtpUser (ej. Gmail).");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
        mensaje.To.Add(MailboxAddress.Parse(destinatarioEmail));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpoPlano };

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_opt.SmtpHost, _opt.SmtpPort, SecureSocketOptions.StartTls, ct);
        await cliente.AuthenticateAsync(_opt.SmtpUser, _opt.SmtpPassword, ct);
        await cliente.SendAsync(mensaje, ct);
        await cliente.DisconnectAsync(true, ct);
        _log.LogInformation("Correo enviado por SMTP a {Destinatario} (asunto: {Asunto}).", destinatarioEmail, asunto);
    }
}
