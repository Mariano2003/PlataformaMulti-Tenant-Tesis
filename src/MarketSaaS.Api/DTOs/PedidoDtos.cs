using System.ComponentModel.DataAnnotations;

namespace MarketSaaS.Api.DTOs;

public class CrearPedidoLineaRequest
{
    [Required, MaxLength(30)]
    public string ProductoId { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }
}

public class CrearPedidoRequest
{
    [Required]
    [MinLength(1)]
    public List<CrearPedidoLineaRequest> Lineas { get; set; } = [];

    [MaxLength(120)]
    public string? ClienteNombre { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string ClienteEmail { get; set; } = null!;

    [MaxLength(40)]
    public string? ClienteTelefono { get; set; }
}

public class PedidoLineaResponse
{
    public string ProductoId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PedidoResponse
{
    public string Id { get; set; } = null!;
    public string NegocioId { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string? MercadoPagoPreferenceId { get; set; }
    public string? MercadoPagoPaymentId { get; set; }
    public string? MercadoPagoStatusDetail { get; set; }
    public IReadOnlyList<PedidoLineaResponse> Lineas { get; set; } = Array.Empty<PedidoLineaResponse>();
    public decimal Total { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public DateTime CreadoEn { get; set; }
}
