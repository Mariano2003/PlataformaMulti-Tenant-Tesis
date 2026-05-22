namespace MarketSaaS.Api.DTOs;

/// <summary>Respuesta al crear una preferencia Checkout Pro para un pedido pendiente.</summary>
public sealed class PreferenciaMercadoPagoResponse
{
    public string PedidoId { get; set; } = null!;
    public string PreferenciaId { get; set; } = null!;
    public string UrlPago { get; set; } = null!;
    public string? UrlPagoSandbox { get; set; }

    /// <summary>URL a la que MP redirige tras pago aprobado (para verificar configuración).</summary>
    public string? UrlRetornoExito { get; set; }
}
