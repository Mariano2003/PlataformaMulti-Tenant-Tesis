namespace MarketSaaS.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MarketSaaS";
    public string Audience { get; set; } = "MarketSaaS";
    public string SigningKey { get; set; } = "";
    public int ExpiresMinutes { get; set; } = 120;
}
