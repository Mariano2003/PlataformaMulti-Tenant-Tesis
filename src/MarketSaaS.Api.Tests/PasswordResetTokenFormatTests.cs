using System.Security.Cryptography;
using MarketSaaS.Api.Infrastructure;

namespace MarketSaaS.Api.Tests;

public sealed class PasswordResetTokenFormatTests
{
    [Fact]
    public void TryParseTokenPlano_acepta_hex_64_valido()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();

        var ok = PasswordResetTokenFormat.TryParseTokenPlano(hex, out var parsed);

        Assert.True(ok);
        Assert.Equal(bytes, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void TryParseTokenPlano_rechaza_formato_invalido(string? token)
    {
        var ok = PasswordResetTokenFormat.TryParseTokenPlano(token, out var parsed);

        Assert.False(ok);
        Assert.Empty(parsed);
    }

    [Fact]
    public void HashBytes_es_sha256_hex_minusculas()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var hash = PasswordResetTokenFormat.HashBytes(bytes);
        var esperado = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        Assert.Equal(esperado, hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Token_generado_y_hash_coinciden_con_flujo_recuperacion()
    {
        var plainBytes = RandomNumberGenerator.GetBytes(32);
        var plainHex = Convert.ToHexString(plainBytes).ToLowerInvariant();

        Assert.True(PasswordResetTokenFormat.TryParseTokenPlano(plainHex, out var parsed));
        Assert.Equal(PasswordResetTokenFormat.HashBytes(plainBytes), PasswordResetTokenFormat.HashBytes(parsed));
    }
}
