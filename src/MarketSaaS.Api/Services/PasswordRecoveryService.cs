using System.Security.Cryptography;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly IMongoCollection<PasswordResetToken> _tokens;
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOpt;
    private readonly MercadoPagoOptions _mpOpt;
    private readonly IAuthService _auth;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<PasswordRecoveryService> _log;

    public PasswordRecoveryService(
        IMongoDatabase db,
        IEmailSender email,
        IOptions<EmailOptions> emailOpt,
        IOptions<MercadoPagoOptions> mpOpt,
        IAuthService auth,
        IHostEnvironment hostEnvironment,
        ILogger<PasswordRecoveryService> log)
    {
        _tokens = db.GetCollection<PasswordResetToken>(CollectionNames.PasswordResetTokens);
        _usuarios = db.GetCollection<Usuario>(CollectionNames.Usuarios);
        _email = email;
        _emailOpt = emailOpt.Value;
        _mpOpt = mpOpt.Value;
        _auth = auth;
        _hostEnvironment = hostEnvironment;
        _log = log;
    }

    public async Task SolicitarAsync(string email, CancellationToken ct = default)
    {
        var emailNorm = email.Trim().ToLowerInvariant();
        var usuario = await _usuarios.Find(u => u.Email == emailNorm && u.Activo).FirstOrDefaultAsync(ct);
        if (usuario is null)
            return;

        var smtpListo = _emailOpt.Enabled &&
            !string.IsNullOrWhiteSpace(_emailOpt.FromEmail) &&
            !string.IsNullOrWhiteSpace(_emailOpt.SmtpUser) &&
            !string.IsNullOrWhiteSpace(_emailOpt.SmtpPassword);

        if (!smtpListo && !_hostEnvironment.IsDevelopment())
        {
            _log.LogWarning(
                "Recuperación de contraseña: correo deshabilitado o SMTP incompleto. No se puede enviar mail a {Email}. Activá Email.Enabled y Gmail (contraseña de aplicación) en appsettings.Development.json y reiniciá la API.",
                emailNorm);
            return;
        }

        if (!smtpListo)
        {
            _log.LogWarning(
                "Recuperación (solo Development): SMTP no configurado — no se manda mail; buscá en consola la línea «DEV — Enlace…» y abrila en el navegador. Ajustá también Email:PublicAppBaseUrl al puerto real de Vite.");
        }

        await _tokens.DeleteManyAsync(t => t.EmailNormalizado == emailNorm, cancellationToken: ct);

        var plainBytes = RandomNumberGenerator.GetBytes(32);
        var plainHex = Convert.ToHexString(plainBytes).ToLowerInvariant();
        var hashHex = HashTokenBytes(plainBytes);

        var minutos = Math.Clamp(_emailOpt.TokenValidMinutes, 15, 1440);
        var entidad = new PasswordResetToken
        {
            Id = ObjectId.GenerateNewId().ToString(),
            EmailNormalizado = emailNorm,
            TokenHash = hashHex,
            ExpiraEnUtc = DateTime.UtcNow.AddMinutes(minutos),
            CreadoEnUtc = DateTime.UtcNow,
        };

        await _tokens.InsertOneAsync(entidad, cancellationToken: ct);

        var baseUrl = ObtenerUrlBaseFront();
        // Token en la ruta (no en ?query) para que Gmail/clientes no corten el enlace con /#/
        var link = FrontAppUrls.Construir(baseUrl, $"/restablecer-clave/{plainHex}");
        var cuerpo =
            $"Hola {usuario.Nombre},\n\n" +
            $"Para elegir una nueva contraseña en MarketSaaS abrí este enlace (caduca en {minutos} minutos):\n\n{link}\n\n" +
            "Si el enlace no se abre al tocarlo, copiá y pegá la línea completa en el navegador.\n\n" +
            "Si no solicitaste este cambio, ignorá este mensaje.\n";

        if (_hostEnvironment.IsDevelopment())
        {
            _log.LogInformation(
                "DEV — Enlace para restablecer contraseña ({Email}): {Link}",
                emailNorm,
                link);
        }

        if (!smtpListo)
            return;

        try
        {
            await _email.EnviarAsync(usuario.Email, "Restablecer contraseña — MarketSaaS", cuerpo, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falló el envío SMTP para recuperación de contraseña ({Email}).", usuario.Email);
            if (_hostEnvironment.IsDevelopment())
            {
                _log.LogWarning(
                    "DEV: el enlace «DEV — Enlace…» de arriba sigue válido aunque Gmail/SMTP haya fallado.");
            }
            else
            {
                await _tokens.DeleteOneAsync(t => t.Id == entidad.Id, ct);
            }
        }
    }

    public async Task RestablecerAsync(string tokenPlano, string nuevaPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

        var tokenNorm = tokenPlano.Trim().ToLowerInvariant();
        if (tokenNorm.Length != 64)
            throw new ArgumentException("El enlace no es válido.");

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromHexString(tokenNorm);
        }
        catch (FormatException)
        {
            throw new ArgumentException("El enlace no es válido.");
        }

        if (tokenBytes.Length != 32)
            throw new ArgumentException("El enlace no es válido.");

        var hashHex = HashTokenBytes(tokenBytes);
        var doc = await _tokens
            .Find(t => t.TokenHash == hashHex && t.ExpiraEnUtc > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);
        if (doc is null)
            throw new ArgumentException("El enlace expiró o no es válido. Solicitá uno nuevo desde «Olvidé mi contraseña».");

        try
        {
            await _auth.ActualizarPasswordPorEmailAsync(doc.EmailNormalizado, nuevaPassword, ct);
        }
        finally
        {
            await _tokens.DeleteManyAsync(t => t.EmailNormalizado == doc.EmailNormalizado, ct);
        }
    }

    private string ObtenerUrlBaseFront()
    {
        var app = _emailOpt.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(app))
            app = _mpOpt.PublicAppBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(app))
            app = "http://localhost:5173";
        return app;
    }

    private static string HashTokenBytes(byte[] tokenBytes) =>
        Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();
}
