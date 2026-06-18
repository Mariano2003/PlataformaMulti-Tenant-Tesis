using System.Security.Cryptography;
using System.Text;
using MarketSaaS.Api.Infrastructure;

namespace MarketSaaS.Api.Tests;

public sealed class MercadoPagoPkceTests
{
    [Fact]
    public void GenerarCodeVerifier_cumple_formato_url_safe()
    {
        var verifier = MercadoPagoPkce.GenerarCodeVerifier();

        Assert.InRange(verifier.Length, 43, 86);
        Assert.DoesNotMatch("[+/=]", verifier);
    }

    [Fact]
    public void GenerarCodeChallengeS256_es_deterministico_y_url_safe()
    {
        const string verifier = "verificador_fijo_para_test_pkce_1234567890";

        var challenge = MercadoPagoPkce.GenerarCodeChallengeS256(verifier);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var esperado = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.Equal(esperado, challenge);
        Assert.DoesNotMatch("[+/=]", challenge);
    }

    [Fact]
    public void GenerarState_es_hex_48_caracteres()
    {
        var state = MercadoPagoPkce.GenerarState();

        Assert.Equal(48, state.Length);
        Assert.Matches("^[0-9a-f]+$", state);
    }
}
