using System.Text.Json.Serialization;

namespace MarketSaaS.Api.DTOs;

public sealed class MercadoPagoOAuthIniciarResponse
{
    public string AuthorizationUrl { get; set; } = null!;
}

internal sealed class MercadoPagoOAuthTokenRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = "";

    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "authorization_code";

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("code_verifier")]
    public string? CodeVerifier { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>Obligatorio en true para vendedores de prueba (sandbox).</summary>
    [JsonPropertyName("test_token")]
    public string? TestToken { get; set; }
}

internal sealed class MercadoPagoOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("public_key")]
    public string? PublicKey { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
