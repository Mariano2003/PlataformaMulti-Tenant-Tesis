using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace MarketSaaS.Api.Infrastructure;

/// <summary>Validación HMAC del header <c>x-signature</c> según documentación de Mercado Pago (webhooks).</summary>
public static class MercadoPagoWebhookSignatureValidator
{
    /// <summary>
    /// Regla MP: si <c>data.id</c> es alfanumérico, en el manifest va en minúsculas; numérico se deja tal cual (trim).
    /// </summary>
    public static string NormalizeDataIdForManifest(string? dataId)
    {
        if (string.IsNullOrWhiteSpace(dataId))
            return "";

        var trimmed = dataId.Trim();
        foreach (var c in trimmed)
        {
            if (char.IsLetter(c))
                return trimmed.ToLowerInvariant();
        }

        return trimmed;
    }

    public static bool TryValidate(
        string? xSignature,
        string? xRequestId,
        string dataIdForManifest,
        string webhookSecret,
        TimeSpan maxTimestampSkew,
        [NotNullWhen(false)] out string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            failureReason = "Secreto vacío.";
            return false;
        }

        if (!TryParseTsAndV1(xSignature, out var ts, out var v1, out failureReason))
            return false;

        if (string.IsNullOrWhiteSpace(xRequestId))
        {
            failureReason = "Falta header x-request-id.";
            return false;
        }

        if (!long.TryParse(ts, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var tsRaw))
        {
            failureReason = "ts inválido en x-signature.";
            return false;
        }

        var tsSeconds = tsRaw > 10_000_000_000L ? tsRaw / 1000 : tsRaw;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - tsSeconds) > (long)maxTimestampSkew.TotalSeconds)
        {
            failureReason = "ts fuera de la ventana permitida (posible replay).";
            return false;
        }

        var normalizedId = NormalizeDataIdForManifest(dataIdForManifest);
        var manifest = $"id:{normalizedId};request-id:{xRequestId.Trim()};ts:{ts};";

        var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
        var messageBytes = Encoding.UTF8.GetBytes(manifest);
        Span<byte> computed = stackalloc byte[32];
        HMACSHA256.HashData(keyBytes, messageBytes, computed);

        byte[] v1Bytes;
        try
        {
            v1Bytes = Convert.FromHexString(v1.Trim());
        }
        catch (FormatException)
        {
            failureReason = "v1 no es hexadecimal válido.";
            return false;
        }

        if (v1Bytes.Length != computed.Length)
        {
            failureReason = "Longitud de firma incorrecta.";
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(computed, v1Bytes))
        {
            failureReason = "Firma HMAC no coincide.";
            return false;
        }

        failureReason = null;
        return true;
    }

    private static bool TryParseTsAndV1(
        string? xSignature,
        out string ts,
        out string v1,
        [NotNullWhen(false)] out string? failureReason)
    {
        ts = "";
        v1 = "";
        if (string.IsNullOrWhiteSpace(xSignature))
        {
            failureReason = "Falta header x-signature.";
            return false;
        }

        foreach (var part in xSignature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0 || eq >= part.Length - 1)
                continue;

            var key = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            if (string.Equals(key, "ts", StringComparison.OrdinalIgnoreCase))
                ts = value;
            else if (string.Equals(key, "v1", StringComparison.OrdinalIgnoreCase))
                v1 = value;
        }

        if (string.IsNullOrEmpty(ts))
        {
            failureReason = "x-signature sin ts.";
            return false;
        }

        if (string.IsNullOrEmpty(v1))
        {
            failureReason = "x-signature sin v1.";
            return false;
        }

        failureReason = null;
        return true;
    }
}
