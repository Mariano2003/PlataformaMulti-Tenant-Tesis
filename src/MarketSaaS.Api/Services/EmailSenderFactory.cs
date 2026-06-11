using MarketSaaS.Api.Options;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

/// <summary>Elige el proveedor según config: Brevo &gt; Resend &gt; SMTP.</summary>
public sealed class EmailSenderFactory : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly MailKitEmailSender _smtp;
    private readonly ResendEmailSender _resend;
    private readonly BrevoEmailSender _brevo;

    public EmailSenderFactory(
        IOptions<EmailOptions> opt,
        MailKitEmailSender smtp,
        ResendEmailSender resend,
        BrevoEmailSender brevo)
    {
        _opt = opt.Value;
        _smtp = smtp;
        _resend = resend;
        _brevo = brevo;
    }

    public Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoPlano, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_opt.BrevoApiKey))
            return _brevo.EnviarAsync(destinatarioEmail, asunto, cuerpoPlano, ct);

        if (!string.IsNullOrWhiteSpace(_opt.ResendApiKey))
            return _resend.EnviarAsync(destinatarioEmail, asunto, cuerpoPlano, ct);

        return _smtp.EnviarAsync(destinatarioEmail, asunto, cuerpoPlano, ct);
    }
}
