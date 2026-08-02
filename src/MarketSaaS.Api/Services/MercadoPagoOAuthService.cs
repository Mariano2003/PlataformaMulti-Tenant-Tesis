using System.Net.Http.Json;
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

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

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
            throw new InvalidOperationException(
                "El enlace de autorización expiró. Volvé a tocar «Conectar con Mercado Pago».");

        // Docs MP: Content-Type application/json + test_token=true para vendedores de prueba.
        // https://www.mercadopago.com.ar/developers/es/docs/security/oauth/creation
        var usarTestToken = _opciones.OAuthTestToken;
        var body = new MercadoPagoOAuthTokenRequest
        {
            ClientId = _opciones.OAuthClientId.Trim(),
            ClientSecret = _opciones.OAuthClientSecret.Trim(),
            GrantType = "authorization_code",
            Code = code.Trim(),
            RedirectUri = ObtenerRedirectUri(),
            TestToken = usarTestToken ? "true" : "false",
        };

        if (_opciones.OAuthUsePkce)
        {
            if (string.IsNullOrWhiteSpace(pendiente.CodeVerifier))
                throw new InvalidOperationException("Falta code_verifier PKCE. Revisá MercadoPago:OAuthUsePkce.");
            body.CodeVerifier = pendiente.CodeVerifier;
        }

        _log.LogInformation(
            "MP OAuth: canjeando code para tienda {Slug} (test_token={Test})",
            pendiente.Slug,
            body.TestToken);

        var token = await SolicitarTokenAsync(body, ct);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            var detalle = HumanizarErrorToken(token);
            _log.LogWarning(
                "MP OAuth callback falló para negocio {NegocioId} (test_token={Test}): {Detalle}",
                pendiente.NegocioId,
                body.TestToken,
                detalle);
            throw new InvalidOperationException(detalle);
        }

        var userId = token.UserId?.ToString();
        await _negocios.GuardarCredencialesOAuthAsync(
            pendiente.NegocioId,
            token.AccessToken,
            token.RefreshToken,
            userId,
            token.ExpiresIn,
            ct);

        await _states.DeleteOneAsync(s => s.Id == pendiente.Id, ct);

        _log.LogInformation(
            "MP OAuth: tienda {Slug} vinculada (user_id={UserId}, test_token={Test})",
            pendiente.Slug,
            userId,
            body.TestToken);

        return pendiente.Slug;
    }

    public async Task<string?> ObtenerSlugPorStateAsync(string state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var pendiente = await _states.Find(s => s.State == state.Trim()).FirstOrDefaultAsync(ct);
        return pendiente?.Slug;
    }

    private static string HumanizarErrorToken(MercadoPagoOAuthTokenResponse? token)
    {
        var raw = (token?.ErrorDescription ?? token?.Message ?? token?.Error ?? "").Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return "No se pudo vincular la cuenta. Si usás un vendedor de prueba, " +
                   "confirmá país Argentina y que la API tenga MercadoPago:OAuthTestToken=true.";
        }

        var lower = raw.ToLowerInvariant();
        if (lower.Contains("invalid_grant") || lower.Contains("authorization_code"))
        {
            return "El código de autorización no es válido o ya se usó. " +
                   "Volvé a tocar «Conectar con Mercado Pago». " +
                   "Con vendedor de prueba necesitás OAuthTestToken=true.";
        }

        if (lower.Contains("redirect_uri"))
        {
            return "La Redirect URI no coincide con Mercado Pago Developers. " +
                   "Debe ser exactamente la URL de la API …/api/mercadopago/oauth/callback";
        }

        return raw.Length > 180 ? raw[..180] + "…" : raw;
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
        MercadoPagoOAuthTokenRequest body,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(nameof(MercadoPagoOAuthService));
        using var response = await http.PostAsJsonAsync(
            "https://api.mercadopago.com/oauth/token",
            body,
            JsonOpts,
            ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        var token = JsonSerializer.Deserialize<MercadoPagoOAuthTokenResponse>(json, JsonOpts);

        if (!response.IsSuccessStatusCode)
        {
            _log.LogWarning(
                "MP OAuth token HTTP {Status}: {Body}",
                (int)response.StatusCode,
                json.Length > 400 ? json[..400] : json);
            return token;
        }

        return token;
    }
}
