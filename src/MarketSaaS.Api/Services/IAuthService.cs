using MarketSaaS.Api.DTOs;

namespace MarketSaaS.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegistrarAsync(RegistroRequest dto, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest dto, CancellationToken ct = default);
}
