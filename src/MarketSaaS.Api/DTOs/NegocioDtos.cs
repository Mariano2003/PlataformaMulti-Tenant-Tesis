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
}

/// <summary>Ejemplo de endpoint tenant-scoped: datos del negocio de la ruta tras validar JWT + slug.</summary>
public class NegocioContextoAdminResponse
{
    public string NegocioId { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }
}
