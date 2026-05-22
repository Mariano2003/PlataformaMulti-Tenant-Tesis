using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

public interface IAuthService
{
    /// <summary>Registro público: solo rol Cliente (sin negocio asociado).</summary>
    Task<AuthResponse> RegistrarClienteAsync(RegistroClienteRequest dto, CancellationToken ct = default);

    /// <summary>Uso interno: SuperAdmin, AdminTienda (alta de tienda) o seed.</summary>
    Task<AuthResponse> RegistrarAsync(RegistroRequest dto, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest dto, CancellationToken ct = default);

    /// <summary>Actualiza contraseña por email normalizado (recuperación por token).</summary>
    Task ActualizarPasswordPorEmailAsync(string emailNormalizado, string nuevaPassword, CancellationToken ct = default);
}
