using MarketSaaS.Api.Models;

namespace MarketSaaS.Api.Services;

public interface ITokenService
{
    (string token, DateTime expiresUtc) CreateToken(Usuario usuario);
}
