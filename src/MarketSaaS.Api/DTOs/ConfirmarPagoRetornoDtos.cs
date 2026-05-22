namespace MarketSaaS.Api.DTOs;

/// <summary>Parámetros que Mercado Pago agrega al volver del Checkout Pro (y opcionales en body).</summary>
public sealed class ConfirmarPagoRetornoRequest
{
    public string? PaymentId { get; set; }
    public string? CollectionId { get; set; }
    public string? ExternalReference { get; set; }
    public string? MerchantOrderId { get; set; }
}

public sealed class ConfirmarPagoRetornoResponse
{
    public bool Procesado { get; set; }
    public string? PedidoId { get; set; }
    public string? EstadoPedido { get; set; }
    public string Mensaje { get; set; } = "";
}
