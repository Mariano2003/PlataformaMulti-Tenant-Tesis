using System.Net.Http.Json;
using System.Text.Json;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Models;
using MarketSaaS.Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketSaaS.Api.Services;

public sealed class MercadoPagoAccessTokenProvider : IMercadoPagoAccessTokenProvider
{
    private static readonly TimeSpan MargenRenovacion = TimeSpan.FromDays(14);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly INegocioService _negocios;
    private readonly MercadoPagoOptions _opciones;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MercadoPagoAccessTokenProvider> _log;

    public MercadoPagoAccessTokenProvider(
        INegocioService negocios,
        IOptions<MercadoPagoOptions> opciones,
        IHttpClientFactory httpClientFactory,
        ILogger<MercadoPagoAccessTokenProvider> log)
    {
        _negocios = negocios;
        _opciones = opciones.Value;
        _httpClientFactory = httpClientFactory;
        _log = log;
    }

    public async Task<string?> ObtenerParaNegocioAsync(Negocio negocio, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(negocio.MercadoPagoAccessToken))
        {
            if (DebeRenovarToken(negocio) && !string.IsNullOrWhiteSpace(negocio.MercadoPagoRefreshToken))
            {
                var renovado = await IntentarRenovarAsync(negocio.Id, ct);
                if (!string.IsNullOrWhiteSpace(renovado))
                    return renovado;
            }

            return negocio.MercadoPagoAccessToken.Trim();
        }

        var global = _opciones.AccessToken?.Trim();
        return string.IsNullOrWhiteSpace(global) ? null : global;
    }

    private static bool DebeRenovarToken(Negocio negocio)
    {
        if (string.IsNullOrWhiteSpace(negocio.MercadoPagoRefreshToken))
            return false;
        if (negocio.MercadoPagoTokenExpiraEn is null)
            return false;
        return negocio.MercadoPagoTokenExpiraEn.Value <= DateTime.UtcNow.Add(MargenRenovacion);
    }

    private async Task<string?> IntentarRenovarAsync(string negocioId, CancellationToken ct)
    {
        var clientId = _opciones.OAuthClientId?.Trim();
        var clientSecret = _opciones.OAuthClientSecret?.Trim();
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var negocio = await _negocios.ObtenerPorIdAsync(negocioId, ct);
        if (negocio is null || string.IsNullOrWhiteSpace(negocio.MercadoPagoRefreshToken))
            return null;

        var body = new MercadoPagoOAuthTokenRequest
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            GrantType = "refresh_token",
            RefreshToken = negocio.MercadoPagoRefreshToken.Trim(),
            TestToken = _opciones.OAuthTestToken ? "true" : "false",
        };

        try
        {
            var token = await SolicitarTokenAsync(body, ct);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                return null;

            await _negocios.GuardarCredencialesOAuthAsync(
                negocioId,
                token.AccessToken,
                token.RefreshToken ?? negocio.MercadoPagoRefreshToken,
                negocio.MercadoPagoUserId,
                token.ExpiresIn,
                ct);

            _log.LogInformation("MP OAuth: access token renovado para negocio {NegocioId}", negocioId);
            return token.AccessToken.Trim();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "MP OAuth: no se pudo renovar token para negocio {NegocioId}", negocioId);
            return negocio.MercadoPagoAccessToken?.Trim();
        }
    }

    private async Task<MercadoPagoOAuthTokenResponse?> SolicitarTokenAsync(
        MercadoPagoOAuthTokenRequest body,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(nameof(MercadoPagoAccessTokenProvider));
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
                json.Length > 300 ? json[..300] : json);
            return null;
        }

        return token;
    }
}
