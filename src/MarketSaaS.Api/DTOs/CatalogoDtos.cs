using System.ComponentModel.DataAnnotations;

namespace MarketSaaS.Api.DTOs;

public class CrearCategoriaRequest
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int? Orden { get; set; }
}

public class ActualizarCategoriaRequest
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}

public class CategoriaResponse
{
    public string Id { get; set; } = null!;
    public string NegocioId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class CrearProductoRequest
{
    /// <summary>ObjectId de categoría del mismo negocio, o null.</summary>
    [MaxLength(30)]
    public string? CategoriaId { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;

    [MaxLength(2000)]
    public string? DescripcionCorta { get; set; }

    public decimal Precio { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public Dictionary<string, string>? Atributos { get; set; }
}

public class ActualizarProductoRequest
{
    [MaxLength(30)]
    public string? CategoriaId { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;

    [MaxLength(2000)]
    public string? DescripcionCorta { get; set; }

    public decimal Precio { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public Dictionary<string, string>? Atributos { get; set; }

    public bool Activo { get; set; } = true;
}

public class ProductoResponse
{
    public string Id { get; set; } = null!;
    public string NegocioId { get; set; } = null!;
    public string? CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? DescripcionCorta { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public Dictionary<string, string>? Atributos { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}
