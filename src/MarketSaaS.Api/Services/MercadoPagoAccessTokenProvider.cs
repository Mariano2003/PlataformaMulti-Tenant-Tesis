using System.Net.Http.Headers;
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

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = negocio.MercadoPagoRefreshToken.Trim(),
        };

        try
        {
            var token = await SolicitarTokenAsync(form, ct);
            if (token is null || string.IsNullOrWhiteSpace(token.Access_token))
                return null;

            await _negocios.GuardarCredencialesOAuthAsync(
                negocioId,
                token.Access_token,
                token.Refresh_token ?? negocio.MercadoPagoRefreshToken,
                negocio.MercadoPagoUserId,
                token.Expires_in,
                ct);

            _log.LogInformation("MP OAuth: access token renovado para negocio {NegocioId}", negocioId);
            return token.Access_token.Trim();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "MP OAuth: no se pudo renovar token para negocio {NegocioId}", negocioId);
            return negocio.MercadoPagoAccessToken?.Trim();
        }
    }

    internal async Task<MercadoPagoOAuthTokenResponse?> SolicitarTokenAsync(
        Dictionary<string, string> form,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(nameof(MercadoPagoAccessTokenProvider));
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
            return null;
        }

        return token;
    }
}
