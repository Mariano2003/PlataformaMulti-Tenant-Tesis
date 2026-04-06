using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MarketSaaS.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;

    public TokenService(IOptions<JwtOptions> jwt) => _jwt = jwt.Value;

    public (string token, DateTime expiresUtc) CreateToken(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey) || _jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 caracteres.");

        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new(ClaimTypes.GivenName, usuario.Nombre),
        };
        if (!string.IsNullOrEmpty(usuario.Apellido))
            claims.Add(new Claim(ClaimTypes.Surname, usuario.Apellido));
        if (!string.IsNullOrEmpty(usuario.NegocioId))
            claims.Add(new Claim("negocio_id", usuario.NegocioId));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
