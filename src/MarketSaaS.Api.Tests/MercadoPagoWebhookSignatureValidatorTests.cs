using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MarketSaaS.Api.Infrastructure;

namespace MarketSaaS.Api.Tests;

public sealed class MercadoPagoWebhookSignatureValidatorTests
{
    [Fact]
    public void TryValidate_acepta_manifest_correcto()
    {
        const string secret = "clave_prueba_tesis";
        const string requestId = "6cb56040-7f0b-45f0-9eb8-09c5e2e3b4f1";
        const string dataId = "AbC12";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var normalized = MercadoPagoWebhookSignatureValidator.NormalizeDataIdForManifest(dataId);
        var manifest = $"id:{normalized};request-id:{requestId};ts:{ts};";
        var v1 = ComputeHmacHex(secret, manifest);
        var xSig = $"ts={ts},v1={v1}";

        var ok = MercadoPagoWebhookSignatureValidator.TryValidate(
            xSig,
            requestId,
            dataId,
            secret,
            TimeSpan.FromMinutes(5),
            out var err);

        Assert.True(ok, err);
        Assert.Null(err);
    }

    [Fact]
    public void TryValidate_rechaza_v1_incorrecto()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var xSig = $"ts={ts},v1=0000000000000000000000000000000000000000000000000000000000000000";

        var ok = MercadoPagoWebhookSignatureValidator.TryValidate(
            xSig,
            "rid",
            "1",
            "secret",
            TimeSpan.FromMinutes(5),
            out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("999", "999")]
    [InlineData("Ab", "ab")]
    public void NormalizeDataIdForManifest(string input, string expected)
    {
        Assert.Equal(expected, MercadoPagoWebhookSignatureValidator.NormalizeDataIdForManifest(input));
    }

    private static string ComputeHmacHex(string secret, string manifest)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var msg = Encoding.UTF8.GetBytes(manifest);
        Span<byte> buf = stackalloc byte[32];
        HMACSHA256.HashData(key, msg, buf);
        return Convert.ToHexString(buf).ToLowerInvariant();
    }
}
