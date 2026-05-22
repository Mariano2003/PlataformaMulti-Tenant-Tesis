namespace MarketSaaS.Api.Options;

/// <summary>Checkout Pro (preferencias). El token sale del panel de desarrolladores de Mercado Pago.</summary>
public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    /// <summary>Access Token de prueba o producción. Vacío deshabilita preferencias y el webhook solo responde OK.</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>URL base pública de esta API (sin barra final), ej. https://tu-api.onrender.com para webhooks.</summary>
    public string PublicApiBaseUrl { get; set; } = "";

    /// <summary>
    /// URL base del front (sin barra final), ej. https://tu-front.onrender.com.
    /// Si está vacía, se usa <see cref="EmailOptions.PublicAppBaseUrl"/>.
    /// Las back URLs de Checkout Pro se arman como /tienda/{slug}?pago=ok|error|pending.
    /// </summary>
    public string PublicAppBaseUrl { get; set; } = "";

    /// <summary>
    /// Plantillas opcionales con <c>{slug}</c> (ej. https://front.com/tienda/{slug}?pago=ok).
    /// Si están vacías, se generan desde <see cref="PublicAppBaseUrl"/>.
    /// </summary>
    public string? BackUrlSuccess { get; set; }
    public string? BackUrlFailure { get; set; }
    public string? BackUrlPending { get; set; }

    /// <summary>
    /// Secreto de firma de webhooks (panel Mercado Pago → Tu integración → Webhooks).
    /// Vacío = no se valida <c>x-signature</c> (útil en local). Si tiene valor, las notificaciones sin firma válida reciben 401.
    /// </summary>
    public string WebhookSecret { get; set; } = "";
}
