using System.Net.Http.Headers;
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
            throw new InvalidOperationException(
                "El enlace de autorización expiró. Volvé a tocar «Conectar con Mercado Pago».");

        // El code de MP es de un solo uso: hay que mandar test_token bien a la primera.
        // true = vendedores/cuentas de prueba (sandbox). false = vendedor real.
        var usarTestToken = _opciones.OAuthTestToken;
        var form = CrearFormToken(code.Trim(), pendiente, usarTestToken);

        _log.LogInformation(
            "MP OAuth: canjeando code para tienda {Slug} (test_token={Test})",
            pendiente.Slug,
            usarTestToken);

        var token = await SolicitarTokenAsync(form, ct);
        if (token is null || string.IsNullOrWhiteSpace(token.Access_token))
        {
            var detalle = HumanizarErrorToken(token);
            _log.LogWarning(
                "MP OAuth callback falló para negocio {NegocioId} (test_token={Test}): {Detalle}",
                pendiente.NegocioId,
                usarTestToken,
                detalle);
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
            "MP OAuth: tienda {Slug} vinculada (user_id={UserId}, test_token={Test})",
            pendiente.Slug,
            userId,
            usarTestToken);

        return pendiente.Slug;
    }

    public async Task<string?> ObtenerSlugPorStateAsync(string state, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var pendiente = await _states.Find(s => s.State == state.Trim()).FirstOrDefaultAsync(ct);
        return pendiente?.Slug;
    }

    private Dictionary<string, string> CrearFormToken(string code, MercadoPagoOAuthState pendiente, bool testToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _opciones.OAuthClientId.Trim(),
            ["client_secret"] = _opciones.OAuthClientSecret.Trim(),
            ["code"] = code,
            ["redirect_uri"] = ObtenerRedirectUri(),
        };

        if (testToken)
            form["test_token"] = "true";

        if (_opciones.OAuthUsePkce)
        {
            if (string.IsNullOrWhiteSpace(pendiente.CodeVerifier))
                throw new InvalidOperationException("Falta code_verifier PKCE. Revisá MercadoPago:OAuthUsePkce.");
            form["code_verifier"] = pendiente.CodeVerifier;
        }

        return form;
    }

    private static string HumanizarErrorToken(MercadoPagoOAuthTokenResponse? token)
    {
        var raw = (token?.Message ?? token?.Error ?? "").Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return "No se pudo vincular la cuenta. Si usás un vendedor de prueba, " +
                   "asegurate de que la API tenga MercadoPago:OAuthTestToken=true y volvé a conectar.";
        }

        var lower = raw.ToLowerInvariant();
        if (lower.Contains("invalid_grant") || lower.Contains("authorization_code"))
        {
            return "El código de autorización no es válido o ya se usó. " +
                   "Volvé a tocar «Conectar con Mercado Pago» e intentá de nuevo. " +
                   "Con vendedor de prueba necesitás OAuthTestToken=true en la API.";
        }

        if (lower.Contains("redirect_uri"))
        {
            return "La Redirect URI no coincide con la configurada en Mercado Pago Developers. " +
                   "Debe ser exactamente: {PublicApiBaseUrl}/api/mercadopago/oauth/callback";
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
        Dictionary<string, string> form,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(nameof(MercadoPagoOAuthService));
        using var content = new FormUrlEncodedContent(form);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

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
