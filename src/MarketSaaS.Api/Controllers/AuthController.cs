using System.Security.Claims;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly INegocioService _negocios;
    private readonly IPasswordRecoveryService _recuperacion;

    public AuthController(IAuthService auth, INegocioService negocios, IPasswordRecoveryService recuperacion)
    {
        _auth = auth;
        _negocios = negocios;
        _recuperacion = recuperacion;
    }

    /// <summary>Alta pública de comprador (Cliente). Dueños de tienda los crea el SuperAdmin al dar de alta el negocio.</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> RegistroCliente([FromBody] RegistroClienteRequest solicitud, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.RegistrarClienteAsync(solicitud, ct));
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
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest solicitud, CancellationToken ct)
    {
        var respuestaLogin = await _auth.LoginAsync(solicitud, ct);
        return respuestaLogin is null ? Unauthorized() : Ok(respuestaLogin);
    }

    /// <summary>Envía enlace por correo (Gmail SMTP u otro) si el email está registrado. Respuesta uniforme por seguridad.</summary>
    [HttpPost("recuperar-clave")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RecuperarClave([FromBody] RecuperarClaveRequest solicitud, CancellationToken ct)
    {
        await _recuperacion.SolicitarAsync(solicitud.Email, ct);
        return Ok(new
        {
            mensaje = "Si el correo está registrado y el envío de mails está configurado, recibirás un enlace para restablecer la contraseña.",
        });
    }

    /// <summary>Define nueva contraseña usando el token recibido por correo.</summary>
    [HttpPost("restablecer-clave")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RestablecerClave([FromBody] RestablecerClaveRequest solicitud, CancellationToken ct)
    {
        try
        {
            await _recuperacion.RestablecerAsync(solicitud.Token, solicitud.NuevaPassword, ct);
            return Ok(new { mensaje = "Contraseña actualizada. Ya podés iniciar sesión." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioPublico), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioPublico>> Me(CancellationToken ct)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var negocioId = User.FindFirst("negocio_id")?.Value;
        string? negocioSlug = null;
        if (!string.IsNullOrEmpty(negocioId))
        {
            var negocio = await _negocios.ObtenerPorIdAsync(negocioId, ct);
            negocioSlug = negocio?.Slug;
        }

        return Ok(new UsuarioPublico
        {
            Id = usuarioId,
            NegocioId = negocioId,
            NegocioSlug = negocioSlug,
            Email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "",
            Nombre = User.FindFirstValue(ClaimTypes.GivenName) ?? "",
            Apellido = User.FindFirstValue(ClaimTypes.Surname),
            Rol = User.FindFirstValue(ClaimTypes.Role) ?? "",
        });
    }
}
