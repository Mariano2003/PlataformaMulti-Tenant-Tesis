using System.Net.Mail;
using MarketSaaS.Api.Authorization;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NegociosController : ControllerBase
{
    private readonly INegocioService _negocios;
    private readonly IAuthService _auth;
    private readonly IMercadoPagoOAuthService _mpOAuth;

    public NegociosController(INegocioService negocios, IAuthService auth, IMercadoPagoOAuthService mpOAuth)
    {
        _negocios = negocios;
        _auth = auth;
        _mpOAuth = mpOAuth;
    }

    /// <summary>Listado público de negocios activos (selector de tienda).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<NegocioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NegocioResponse>>> ListarActivos(CancellationToken ct)
    {
        var lista = await _negocios.ListarActivosOrdenadosAsync(ct);
        return Ok(lista.Select(ToResponse).ToList());
    }

    /// <summary>Obtiene un negocio por su slug (URL pública).</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NegocioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NegocioResponse>> PorSlug(string slug, CancellationToken ct)
    {
        var negocio = await _negocios.ObtenerPorSlugAsync(slug, ct);
        if (negocio is null)
            return NotFound();

        return Ok(ToResponse(negocio));
    }

    /// <summary>
    /// Verifica JWT + que el <c>negocio_id</c> del token corresponda al <paramref name="slug"/> (SuperAdmin puede cualquier slug existente).
    /// Plantilla para futuros <c>.../admin/...</c>.
    /// </summary>
    [HttpGet("{slug}/admin/contexto")]
    [Authorize(Policy = Policies.SuperAdminOrAdminTienda)]
    [RequireMatchingNegocio]
    [ProducesResponseType(typeof(NegocioContextoAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<NegocioContextoAdminResponse> ContextoAdmin()
    {
        if (!HttpContext.TryGetNegocioActual(out var negocio))
            return NotFound();

        return Ok(new NegocioContextoAdminResponse
        {
            NegocioId = negocio.Id,
            Slug = negocio.Slug,
            Nombre = negocio.Nombre,
            Activo = negocio.Activo,
            MercadoPagoTiendaConfigurado = !string.IsNullOrWhiteSpace(negocio.MercadoPagoAccessToken),
            // Refresh token o fecha de OAuth: algunos tokens TEST no traen refresh pero sí quedan vinculados.
            MercadoPagoConectadoOAuth = !string.IsNullOrWhiteSpace(negocio.MercadoPagoRefreshToken)
                || negocio.MercadoPagoConectadoEn.HasValue,
            MercadoPagoOAuthDisponible = _mpOAuth.ConnectHabilitado,
            MercadoPagoUserId = negocio.MercadoPagoUserId,
            MercadoPagoConectadoEn = negocio.MercadoPagoConectadoEn,
        });
    }

    /// <summary>
    /// Alta de negocio (tenant). Solo <see cref="Roles.SuperAdmin"/>.
    /// Opcionalmente crea el usuario <see cref="Roles.AdminTienda"/> inicial (<c>TiendaAdminEmail</c> + contraseña + nombre).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType(typeof(NegocioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NegocioResponse>> Crear([FromBody] CrearNegocioRequest solicitud, CancellationToken ct)
    {
        var emailAdmin = solicitud.TiendaAdminEmail?.Trim();
        var quiereDueño = !string.IsNullOrWhiteSpace(emailAdmin);
        if (quiereDueño)
        {
            try
            {
                _ = new MailAddress(emailAdmin!);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Email del dueño no válido." });
            }

            if (string.IsNullOrWhiteSpace(solicitud.TiendaAdminPassword)
                || solicitud.TiendaAdminPassword.Length < 8)
                return BadRequest(new
                {
                    error = "Contraseña del dueño: mínimo 8 caracteres.",
                });

            if (string.IsNullOrWhiteSpace(solicitud.TiendaAdminNombre))
                return BadRequest(new { error = "Nombre del dueño es obligatorio si indicás su email." });
        }

        Models.Negocio negocioCreado;
        try
        {
            negocioCreado = await _negocios.CrearAsync(solicitud, ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        var respuesta = ToResponse(negocioCreado);

        if (!quiereDueño)
            return CreatedAtAction(nameof(PorSlug), new { slug = negocioCreado.Slug }, respuesta);

        try
        {
            await _auth.RegistrarAsync(
                new RegistroRequest
                {
                    Email = emailAdmin!.ToLowerInvariant(),
                    Password = solicitud.TiendaAdminPassword!,
                    Nombre = solicitud.TiendaAdminNombre!.Trim(),
                    Apellido = solicitud.TiendaAdminApellido?.Trim(),
                    Rol = Roles.AdminTienda,
                    NegocioId = negocioCreado.Id,
                },
                ct);
        }
        catch (ArgumentException ex)
        {
            await _negocios.EliminarPorIdAsync(negocioCreado.Id, ct);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await _negocios.EliminarPorIdAsync(negocioCreado.Id, ct);
            return Conflict(new { error = ex.Message });
        }

        respuesta.AdminTiendaCreado = true;
        respuesta.AdminTiendaEmail = emailAdmin!.ToLowerInvariant();
        return CreatedAtAction(nameof(PorSlug), new { slug = negocioCreado.Slug }, respuesta);
    }

    private static NegocioResponse ToResponse(Models.Negocio negocio) => new()
    {
        Id = negocio.Id,
        Slug = negocio.Slug,
        Nombre = negocio.Nombre,
        DescripcionCorta = negocio.DescripcionCorta,
        LogoUrl = negocio.LogoUrl,
        Activo = negocio.Activo,
        CreadoEn = negocio.CreadoEn,
    };
}
