using System.Security.Claims;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Registro([FromBody] RegistroRequest dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.RegistrarAsync(dto, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest dto, CancellationToken ct)
    {
        var res = await _auth.LoginAsync(dto, ct);
        return res is null ? Unauthorized() : Ok(res);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioPublico), StatusCodes.Status200OK)]
    public ActionResult<UsuarioPublico> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(id))
            return Unauthorized();

        return Ok(new UsuarioPublico
        {
            Id = id,
            NegocioId = User.FindFirst("negocio_id")?.Value,
            Email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "",
            Nombre = User.FindFirstValue(ClaimTypes.GivenName) ?? "",
            Apellido = User.FindFirstValue(ClaimTypes.Surname),
            Rol = User.FindFirstValue(ClaimTypes.Role) ?? "",
        });
    }
}
