using System.Text.Json;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class MercadoPagoOAuthService : IMercadoPagoOAuthService
{
    private static readonly TimeSpan ValidezState = TimeSpan.FromMinutes(10);

    private readonly IMongoCollection<MercadoPagoOAuthState> _states;
    private readonly INegocioService _negocios;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MercadoPagoOptions _opciones;
    private readonly ILogger<MercadoPagoOAuthService> _log;

    public MercadoPagoOAuthService(
        IMongoDatabase db,
        INegocioService negocios,
        IHttpClientFactory httpClientFactory,
        IOptions<MercadoPagoOptions> opciones,
        ILogger<MercadoPagoOAuthService> log)
    {
        _states = db.GetCollection<MercadoPagoOAuthState>(CollectionNames.MercadoPagoOAuthStates);
        _negocios = negocios;
        _httpClientFactory = httpClientFactory;
        _opciones = opciones.Value;
        _log = log;
    }

    public bool ConnectHabilitado =>
        !string.IsNullOrWhiteSpace(_opciones.OAuthClientId)
        && !string.IsNullOrWhiteSpace(_opciones.OAuthClientSecret)
        && !string.IsNullOrWhiteSpace(ObtenerRedirectUri());

    public async Task<string> IniciarAutorizacionAsync(string negocioId, string slug, CancellationToken ct = default)
    {
        if (!ConnectHabilitado)
            throw new InvalidOperationException(
                "Mercado Pago Connect no está configurado en la API (OAuthClientId, OAuthClientSecret y PublicApiBaseUrl).");

        var state = MercadoPagoPkce.GenerarState();
        string? codeVerifier = null;
        string? codeChallenge = null;

        if (_opciones.OAuthUsePkce)
        {
            codeVerifier = MercadoPagoPkce.GenerarCodeVerifier();
            codeChallenge = MercadoPagoPkce.GenerarCodeChallengeS256(codeVerifier);
        }

        var doc = new MercadoPagoOAuthState
        {
            Id = ObjectId.GenerateNewId().ToString(),
            State = state,
            NegocioId = negocioId,
            Slug = slug.Trim().ToLowerInvariant(),
            CodeVerifier = codeVerifier,
            CreadoEn = DateTime.UtcNow,
            ExpiraEn = DateTime.UtcNow.Add(ValidezState),
        };
        await _states.InsertOneAsync(doc, cancellationToken: ct);

        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _opciones.OAuthClientId.Trim(),
            ["redirect_uri"] = ObtenerRedirectUri(),
            ["state"] = state,
            ["platform_id"] = "mp",
        };

        if (_opciones.OAuthUsePkce && codeChallenge is not null)
        {
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = "S256";
        }

        return QueryHelpers.AddQueryString("https://auth.mercadopago.com/authorization", query!);
    }

    public async Task<string> CompletarAutorizacionAsync(string code, string state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("Código o state de OAuth inválido.");

        var pendiente = await _states.Find(s => s.State == state.Trim()).FirstOrDefaultAsync(ct);
        if (pendiente is null || pendiente.ExpiraEn < DateTime.UtcNow)
            throw new InvalidOperationException("El enlace de autorización expiró. Volvé a intentar desde el panel.");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _opciones.OAuthClientId.Trim(),
            ["client_secret"] = _opciones.OAuthClientSecret.Trim(),
            ["code"] = code.Trim(),
            ["redirect_uri"] = ObtenerRedirectUri(),
        };

        if (_opciones.OAuthUsePkce)
        {
            if (string.IsNullOrWhiteSpace(pendiente.CodeVerifier))
                throw new InvalidOperationException("Falta code_verifier PKCE.");
            form["code_verifier"] = pendiente.CodeVerifier;
        }

        var token = await SolicitarTokenAsync(form, ct);
        if (token is null || string.IsNullOrWhiteSpace(token.Access_token))
        {
            var detalle = token?.Message ?? token?.Error ?? "No se pudo obtener el access token.";
            _log.LogWarning("MP OAuth callback falló para negocio {NegocioId}: {Detalle}", pendiente.NegocioId, detalle);
            throw new InvalidOperationException(detalle);
        }

        var userId = token.User_id?.ToString();
        await _negocios.GuardarCredencialesOAuthAsync(
            pendiente.NegocioId,
            token.Access_token,
            token.Refresh_token,
            userId,
            token.Expires_in,
            ct);

        await _states.DeleteOneAsync(s => s.Id == pendiente.Id, ct);

        _log.LogInformation(
            "MP OAuth: tienda {Slug} vinculada (user_id={UserId})",
            pendiente.Slug,
            userId);

        return pendiente.Slug;
    }

    public async Task<string?> ObtenerSlugPorStateAsync(string state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var pendiente = await _states.Find(s => s.State == state.Trim()).FirstOrDefaultAsync(ct);
        return pendiente?.Slug;
    }

    private string ObtenerRedirectUri()
    {
        var custom = _opciones.OAuthRedirectUri?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(custom))
            return custom;

        var apiBase = _opciones.PublicApiBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(apiBase))
            throw new InvalidOperationException("MercadoPago:PublicApiBaseUrl es obligatorio para OAuth Connect.");

        return $"{apiBase}/api/mercadopago/oauth/callback";
    }

    private async Task<MercadoPagoOAuthTokenResponse?> SolicitarTokenAsync(
        Dictionary<string, string> form,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(nameof(MercadoPagoOAuthService));
        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync("https://api.mercadopago.com/oauth/token", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        var token = JsonSerializer.Deserialize<MercadoPagoOAuthTokenResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (!response.IsSuccessStatusCode)
        {
            _log.LogWarning(
                "MP OAuth token HTTP {Status}: {Body}",
                (int)response.StatusCode,
                json.Length > 300 ? json[..300] : json);
            return token;
        }

        return token;
    }
}
