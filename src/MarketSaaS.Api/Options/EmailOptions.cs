namespace MarketSaaS.Api.Options;

/// <summary>SMTP (ej. Gmail con contraseña de aplicación). Si <see cref="Enabled"/> es false, no se envían mails.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }

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
