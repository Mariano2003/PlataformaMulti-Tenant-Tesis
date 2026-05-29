namespace MarketSaaS.Api.Options;

/// <summary>
/// Correo: SMTP (local) o Resend HTTP API (recomendado en Render gratis: bloquea puertos 587/465).
/// Si <see cref="ResendApiKey"/> tiene valor, se usa Resend en lugar de SMTP.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }

    /// <summary>API key de <a href="https://resend.com">Resend</a> (re_...). Prioridad sobre SMTP.</summary>
    public string ResendApiKey { get; set; } = "";

    /// <summary>Remitente Resend, ej. <c>MarketSaaS &lt;onboarding@resend.dev&gt;</c> o tu email verificado en Resend.</summary>
    public string ResendFrom { get; set; } = "";

    public string SmtpHost { get; set; } = "smtp.gmail.com";

    public int SmtpPort { get; set; } = 587;

    /// <summary>URL del front para el enlace del mail (ej. http://localhost:5173 o el puerto que use Vite).</summary>
    public string PublicAppBaseUrl { get; set; } = "http://localhost:5173";

    public string FromEmail { get; set; } = "";

    public string FromName { get; set; } = "MarketSaaS";

    /// <summary>Cuenta Gmail (o usuario SMTP).</summary>
    public string SmtpUser { get; set; } = "";

    /// <summary>Contraseña de aplicación de Google (no la clave de la cuenta).</summary>
    public string SmtpPassword { get; set; } = "";

    /// <summary>Validez del token de recuperación.</summary>
    public int TokenValidMinutes { get; set; } = 60;
}
