using System.ComponentModel.DataAnnotations;

namespace MarketSaaS.Api.DTOs;

public class CrearNegocioRequest
{
    [Required, MaxLength(80)]
    public string Slug { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;

    [MaxLength(500)]
    public string? DescripcionCorta { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(4000)]
    public string? TemaJson { get; set; }

    [MaxLength(200)]
    public string? EmailContacto { get; set; }

    /// <summary>Si se informan junto con contraseña y nombre, se registra un usuario AdminTienda para este negocio.</summary>
    /// <remarks>Sin validadores que actúen con campo vacío: la validación condicional está en el controlador.</remarks>
    [MaxLength(200)]
    public string? TiendaAdminEmail { get; set; }

    [MaxLength(200)]
    public string? TiendaAdminPassword { get; set; }

    [MaxLength(100)]
    public string? TiendaAdminNombre { get; set; }

    [MaxLength(100)]
    public string? TiendaAdminApellido { get; set; }
}

public class NegocioResponse
{
    public string Id { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? DescripcionCorta { get; set; }
    public string? LogoUrl { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }

    /// <summary>True solo en respuesta de alta cuando se creó el usuario inicial.</summary>
    public bool AdminTiendaCreado { get; set; }

    public string? AdminTiendaEmail { get; set; }
}

/// <summary>Ejemplo de endpoint tenant-scoped: datos del negocio de la ruta tras validar JWT + slug.</summary>
public class NegocioContextoAdminResponse
{
    public string NegocioId { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }

    /// <summary>True si la tienda tiene Access Token propio de Mercado Pago (los cobros van a esa cuenta).</summary>
    public bool MercadoPagoTiendaConfigurado { get; set; }
}

/// <summary>Actualización parcial: propiedades omitidas en JSON no modifican el valor guardado.</summary>
public class ActualizarMercadoPagoNegocioRequest
{
    /// <summary>Nuevo Access Token. Si se envía cadena vacía, se borra y la tienda usa el token global de la API.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Secreto del webhook en MP para esta cuenta. Vacío borra el secreto por tienda (se usa el global si existe).</summary>
    public string? WebhookSecret { get; set; }
}
