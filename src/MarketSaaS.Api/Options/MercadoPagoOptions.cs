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

    /// <summary>APP_ID de la aplicación **de la plataforma** en Mercado Pago Developers (OAuth Connect).</summary>
    public string OAuthClientId { get; set; } = "";

    /// <summary>SECRET_KEY de esa aplicación.</summary>
    public string OAuthClientSecret { get; set; } = "";

    /// <summary>
    /// Redirect URI registrada en MP. Si está vacía: <c>{PublicApiBaseUrl}/api/mercadopago/oauth/callback</c>.
    /// Debe coincidir exactamente con la URL en el panel de la app.
    /// </summary>
    public string? OAuthRedirectUri { get; set; }

    /// <summary>Si true, se envía PKCE (<c>code_challenge</c> S256). Activá el mismo flujo en el panel MP.</summary>
    public bool OAuthUsePkce { get; set; }

    /// <summary>
    /// Si true (default), las redirecciones al front usan <c>/#/ruta</c> (router hash en Render).
    /// Poné false solo si el front se buildó con <c>VITE_ROUTER_HASH=false</c> y tenés rewrite SPA.
    /// </summary>
    public bool SpaUseHashRouter { get; set; } = true;
}
