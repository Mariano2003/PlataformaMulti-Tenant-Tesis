using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using MarketSaaS.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Infrastructure;

/// <summary>
/// Crea usuarios de prueba con roles SuperAdmin, AdminTienda y Cliente (solo Development).
/// Los roles en sí son constantes en código + JWT; aquí se persisten documentos en <c>usuarios</c>.
/// </summary>
public sealed class DevIdentitySeedHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IHostEnvironment _env;
    private readonly IOptions<DevSeedOptions> _options;
    private readonly ILogger<DevIdentitySeedHostedService> _log;

    public DevIdentitySeedHostedService(
        IServiceProvider sp,
        IHostEnvironment env,
        IOptions<DevSeedOptions> options,
        ILogger<DevIdentitySeedHostedService> log)
    {
        _sp = sp;
        _env = env;
        _options = options;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var opt = _options.Value;
        if (!opt.Enabled || !_env.IsDevelopment())
            return;

        using var scope = _sp.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var negocios = scope.ServiceProvider.GetRequiredService<INegocioService>();

        await TryRegistrarAsync(
            auth,
            new RegistroRequest
            {
                Email = opt.SuperAdminEmail.Trim(),
                Password = opt.SuperAdminPassword,
                Nombre = "Admin",
                Apellido = "Plataforma",
                Rol = Roles.SuperAdmin,
            },
            "SuperAdmin",
            cancellationToken);

        if (!opt.SeedDemoTienda)
            return;

        var slug = opt.DemoNegocioSlug.Trim();
        var negocio = await negocios.ObtenerPorSlugAsync(slug, cancellationToken);
        if (negocio is null)
        {
            try
            {
                negocio = await negocios.CrearAsync(
                    new CrearNegocioRequest
                    {
                        Slug = slug,
                        Nombre = opt.DemoNegocioNombre.Trim(),
                        DescripcionCorta = "Semilla desarrollo",
                        EmailContacto = "demo@local.test",
                    },
                    cancellationToken);
                _log.LogInformation(
                    "DevSeed: negocio demo '{Slug}' creado.",
                    negocio.Slug);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "DevSeed: no se pudo crear el negocio demo '{Slug}'.", slug);
                negocio = await negocios.ObtenerPorSlugAsync(slug, cancellationToken);
            }
        }

        if (negocio is null)
            return;

        await TryRegistrarAsync(
            auth,
            new RegistroRequest
            {
                Email = opt.AdminTiendaEmail.Trim(),
                Password = opt.AdminTiendaPassword,
                Nombre = "Dueño",
                Rol = Roles.AdminTienda,
                NegocioId = negocio.Id,
            },
            "AdminTienda",
            cancellationToken);

        await TryRegistrarAsync(
            auth,
            new RegistroRequest
            {
                Email = opt.ClienteEmail.Trim(),
                Password = opt.ClientePassword,
                Nombre = "Cliente",
                Rol = Roles.Cliente,
                NegocioId = null,
            },
            "Cliente (sin negocio)",
            cancellationToken);
    }

    private async Task TryRegistrarAsync(
        IAuthService auth,
        RegistroRequest solicitud,
        string etiqueta,
        CancellationToken ct)
    {
        try
        {
            await auth.RegistrarAsync(solicitud, ct);
            _log.LogInformation(
                "DevSeed: usuario {Etiqueta} creado ({Email}).",
                etiqueta,
                solicitud.Email);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogDebug(ex, "DevSeed: omitido {Etiqueta} ({Email}).", etiqueta, solicitud.Email);
        }
        catch (ArgumentException ex)
        {
            _log.LogWarning(ex, "DevSeed: datos inválidos para {Etiqueta}.", etiqueta);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
