namespace MarketSaaS.Api.Services;

public interface IImageStorageService
{
    Task<string> GuardarImagenProductoAsync(
        string negocioId,
        Stream contenido,
        string contentType,
        string nombreOriginal,
        CancellationToken ct = default);
}
