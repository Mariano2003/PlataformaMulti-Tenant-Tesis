using System.Security.Cryptography;

namespace MarketSaaS.Api.Infrastructure;

/// <summary>Formato y hash del token de recuperación de contraseña (64 hex = 32 bytes).</summary>
public static class PasswordResetTokenFormat
{
    public static bool TryParseTokenPlano(string? tokenPlano, out byte[] tokenBytes)
    {
        tokenBytes = [];
        if (string.IsNullOrWhiteSpace(tokenPlano))
            return false;

        var tokenNorm = tokenPlano.Trim().ToLowerInvariant();
        if (tokenNorm.Length != 64)
            return false;

        try
        {
            tokenBytes = Convert.FromHexString(tokenNorm);
        }
        catch (FormatException)
        {
            return false;
        }

        return tokenBytes.Length == 32;
    }

    public static string HashBytes(ReadOnlySpan<byte> tokenBytes) =>
        Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();
}
