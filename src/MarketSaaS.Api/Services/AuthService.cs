using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IMongoCollection<Negocio> _negocios;
    private readonly ITokenService _tokens;

    public AuthService(IMongoDatabase db, ITokenService tokens)
    {
        _usuarios = db.GetCollection<Usuario>(CollectionNames.Usuarios);
        _negocios = db.GetCollection<Negocio>(CollectionNames.Negocios);
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegistrarAsync(RegistroRequest dto, CancellationToken ct = default)
    {
        if (!Roles.IsValid(dto.Rol))
            throw new ArgumentException($"Rol inválido. Usar: {Roles.SuperAdmin}, {Roles.AdminTienda}, {Roles.Cliente}.");

        var email = dto.Email.Trim().ToLowerInvariant();

        if (dto.Rol == Roles.SuperAdmin)
        {
            if (!string.IsNullOrEmpty(dto.NegocioId))
                throw new ArgumentException("SuperAdmin no debe tener NegocioId.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.NegocioId))
                throw new ArgumentException("AdminTienda y Cliente requieren NegocioId.");
            if (!ObjectId.TryParse(dto.NegocioId, out _))
                throw new ArgumentException("NegocioId no es un ObjectId válido.");
            var negocio = await _negocios.Find(n => n.Id == dto.NegocioId).FirstOrDefaultAsync(ct);
            if (negocio is null)
                throw new InvalidOperationException("El negocio indicado no existe.");
        }

        var existeEmail = await _usuarios.Find(u => u.Email == email).AnyAsync(ct);
        if (existeEmail)
            throw new InvalidOperationException("El email ya está registrado.");

        var usuario = new Usuario
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NegocioId = dto.Rol == Roles.SuperAdmin ? null : dto.NegocioId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Rol = dto.Rol,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
        };

        await _usuarios.InsertOneAsync(usuario, cancellationToken: ct);
        return BuildAuthResponse(usuario);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarios.Find(u => u.Email == email).FirstOrDefaultAsync(ct);
        if (usuario is null || !usuario.Activo)
            return null;
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return null;

        return BuildAuthResponse(usuario);
    }

    private AuthResponse BuildAuthResponse(Usuario u)
    {
        var (token, expiraEnUtc) = _tokens.CreateToken(u);
        return new AuthResponse
        {
            Token = token,
            ExpiraEn = expiraEnUtc,
            Usuario = new UsuarioPublico
            {
                Id = u.Id,
                NegocioId = u.NegocioId,
                Email = u.Email,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Rol = u.Rol,
            },
        };
    }
}
