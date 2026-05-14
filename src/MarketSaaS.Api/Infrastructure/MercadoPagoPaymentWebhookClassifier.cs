namespace MarketSaaS.Api.Infrastructure;

/// <summary>Decide qué hacer con el estado (<c>status</c>) de un pago de Mercado Pago recibido por webhook.</summary>
public static class MercadoPagoPaymentWebhookClassifier
{
    public enum ResultadoNotificacionPago
    {
        /// <summary>Seguir esperando (p. ej. <c>pending</c>, <c>in_process</c>) o estado no manejado.</summary>
        Ignorar = 0,

        /// <summary>Confirmar pedido y stock.</summary>
        Aprobado = 1,

        /// <summary>Cerrar pedido <c>PendientePago</c> como fallido (sin tocar stock).</summary>
        RechazoTerminal = 2,
    }

    public static ResultadoNotificacionPago Clasificar(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return ResultadoNotificacionPago.Ignorar;

        if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
            return ResultadoNotificacionPago.Aprobado;

        if (EsTransitorio(status))
            return ResultadoNotificacionPago.Ignorar;

        if (EsRechazoTerminal(status))
            return ResultadoNotificacionPago.RechazoTerminal;

        return ResultadoNotificacionPago.Ignorar;
    }

    private static bool EsTransitorio(string status) =>
        status.Equals("pending", StringComparison.OrdinalIgnoreCase)
        || status.Equals("in_process", StringComparison.OrdinalIgnoreCase)
        || status.Equals("in_mediation", StringComparison.OrdinalIgnoreCase)
        || status.Equals("authorized", StringComparison.OrdinalIgnoreCase);

    private static bool EsRechazoTerminal(string status) =>
        status.Equals("rejected", StringComparison.OrdinalIgnoreCase)
        || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
        || status.Equals("refunded", StringComparison.OrdinalIgnoreCase)
        || status.Equals("charged_back", StringComparison.OrdinalIgnoreCase);
}
