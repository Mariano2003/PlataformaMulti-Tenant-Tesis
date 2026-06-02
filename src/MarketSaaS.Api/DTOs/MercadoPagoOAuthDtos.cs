namespace MarketSaaS.Api.DTOs;

public sealed class MercadoPagoOAuthIniciarResponse
{
    public string AuthorizationUrl { get; set; } = null!;
}

internal sealed class MercadoPagoOAuthTokenResponse
{
    public string? Access_token { get; set; }
    public string? Refresh_token { get; set; }
    public string? Public_key { get; set; }
    public long? User_id { get; set; }
    public int? Expires_in { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
