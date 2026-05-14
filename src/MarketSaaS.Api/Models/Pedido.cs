using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarketSaaS.Api.Models;

public static class PedidoEstados
{
    /// <summary>Esperando pago en Mercado Pago; el stock aún no se descuenta.</summary>
    public const string PendientePago = "PendientePago";

    /// <summary>Webhook tomó el pedido para descontar stock (evita doble procesamiento concurrente).</summary>
    public const string ProcesandoPago = "ProcesandoPago";

    public const string Pagado = "Pagado";
    public const string Rechazado = "Rechazado";

    /// <summary>Flujo legacy / contrareembolso sin pasarela (datos viejos en BD).</summary>
    public const string Confirmado = "Confirmado";
}

/// <summary>Ítem persistido en el pedido (snapshot de precio y nombre).</summary>
public class PedidoLinea
{
    [BsonElement("productoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductoId { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    [BsonElement("precioUnitario")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal PrecioUnitario { get; set; }

    [BsonElement("subtotal")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Subtotal { get; set; }
}

public class Pedido
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("negocioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string NegocioId { get; set; } = null!;

    [BsonElement("estado")]
    public string Estado { get; set; } = PedidoEstados.PendientePago;

    [BsonElement("mercadoPagoPreferenceId")]
    public string? MercadoPagoPreferenceId { get; set; }

    /// <summary>Id del pago aprobado en MP (texto para evitar líos de serialización).</summary>
    [BsonElement("mercadoPagoPaymentId")]
    public string? MercadoPagoPaymentId { get; set; }

    /// <summary>Detalle del estado del pago en MP (p. ej. <c>cc_rejected_insufficient_amount</c>).</summary>
    [BsonElement("mercadoPagoStatusDetail")]
    [BsonIgnoreIfNull]
    public string? MercadoPagoStatusDetail { get; set; }

    [BsonElement("lineas")]
    public List<PedidoLinea> Lineas { get; set; } = [];

    [BsonElement("total")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Total { get; set; }

    [BsonElement("clienteNombre")]
    public string? ClienteNombre { get; set; }

    [BsonElement("clienteEmail")]
    public string? ClienteEmail { get; set; }

    [BsonElement("clienteTelefono")]
    public string? ClienteTelefono { get; set; }

    [BsonElement("creadoEn")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
