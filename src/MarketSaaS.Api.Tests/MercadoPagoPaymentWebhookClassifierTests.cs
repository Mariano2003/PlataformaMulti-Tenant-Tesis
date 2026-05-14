using MarketSaaS.Api.Infrastructure;
using Xunit;

namespace MarketSaaS.Api.Tests;

public sealed class MercadoPagoPaymentWebhookClassifierTests
{
    [Theory]
    [InlineData("approved", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Aprobado)]
    [InlineData("APPROVED", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Aprobado)]
    [InlineData("rejected", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal)]
    [InlineData("cancelled", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal)]
    [InlineData("refunded", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal)]
    [InlineData("charged_back", MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.RechazoTerminal)]
    public void Clasificar_terminales_y_aprobado(string status, MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago esperado)
    {
        Assert.Equal(esperado, MercadoPagoPaymentWebhookClassifier.Clasificar(status));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("in_process")]
    [InlineData("in_mediation")]
    [InlineData("authorized")]
    public void Clasificar_transitorios_ignora(string status)
    {
        Assert.Equal(
            MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Ignorar,
            MercadoPagoPaymentWebhookClassifier.Clasificar(status));
    }

    [Fact]
    public void Clasificar_vacio_o_desconocido_ignora()
    {
        Assert.Equal(
            MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Ignorar,
            MercadoPagoPaymentWebhookClassifier.Clasificar(null));
        Assert.Equal(
            MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Ignorar,
            MercadoPagoPaymentWebhookClassifier.Clasificar("   "));
        Assert.Equal(
            MercadoPagoPaymentWebhookClassifier.ResultadoNotificacionPago.Ignorar,
            MercadoPagoPaymentWebhookClassifier.Clasificar("future_status_xyz"));
    }
}
