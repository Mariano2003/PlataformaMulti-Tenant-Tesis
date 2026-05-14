namespace MarketSaaS.Api.Services;

public interface IPasswordRecoveryService
{
    /// <summary>No revela si el email existe; solo envía si hay usuario activo y SMTP configurado.</summary>
    Task SolicitarAsync(string email, CancellationToken ct = default);

    Task RestablecerAsync(string tokenPlano, string nuevaPassword, CancellationToken ct = default);
}
