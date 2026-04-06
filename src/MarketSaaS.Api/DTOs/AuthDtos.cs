using System.ComponentModel.DataAnnotations;

namespace MarketSaaS.Api.DTOs;

public class RegistroRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [MaxLength(100)]
    public string? Apellido { get; set; }

    [MaxLength(40)]
    public string? Telefono { get; set; }

    /// <summary>SuperAdmin | AdminTienda | Cliente</summary>
    [Required, MaxLength(40)]
    public string Rol { get; set; } = null!;

    /// <summary>Requerido si el rol no es SuperAdmin (ObjectId del negocio).</summary>
    public string? NegocioId { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiraEn { get; set; }
    public UsuarioPublico Usuario { get; set; } = null!;
}

public class UsuarioPublico
{
    public string Id { get; set; } = null!;
    public string? NegocioId { get; set; }
    public string Email { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Apellido { get; set; }
    public string Rol { get; set; } = null!;
}
