namespace MarketSaaS.Api.Options;

/// <summary>Checkout Pro (preferencias). El token sale del panel de desarrolladores de Mercado Pago.</summary>
public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    /// <summary>Access Token de prueba o producción. Vacío deshabilita preferencias y el webhook solo responde OK.</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>URL base pública de esta API (sin barra final), ej. https://xxxx.ngrok-free.app para que MP llame al webhook en desarrollo.</summary>
    public string PublicApiBaseUrl { get; set; } = "";

    /// <summary>URLs de retorno post-pago (Checkout Pro). Si quedan vacías, MP usa valores por defecto del panel.</summary>
    public string? BackUrlSuccess { get; set; }
    public string? BackUrlFailure { get; set; }
    public string? BackUrlPending { get; set; }

    /// <summary>
    /// Secreto de firma de webhooks (panel Mercado Pago → Tu integración → Webhooks).
    /// Vacío = no se valida <c>x-signature</c> (útil en local). Si tiene valor, las notificaciones sin firma válida reciben 401.
    /// </summary>
    public string WebhookSecret { get; set; } = "";
}
