using MarketSaaS.Api.Options;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class EmailSenderFactory : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly MailKitEmailSender _smtp;
    private readonly ResendEmailSender _resend;

    public EmailSenderFactory(
        IOptions<EmailOptions> opt,
        MailKitEmailSender smtp,
        ResendEmailSender resend)
    {
        _opt = opt.Value;
        _smtp = smtp;
        _resend = resend;
    }

    public Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoPlano, CancellationToken ct = default)
    {
        var usarResend = !string.IsNullOrWhiteSpace(_opt.ResendApiKey);
        return usarResend
            ? _resend.EnviarAsync(destinatarioEmail, asunto, cuerpoPlano, ct)
            : _smtp.EnviarAsync(destinatarioEmail, asunto, cuerpoPlano, ct);
    }
}
