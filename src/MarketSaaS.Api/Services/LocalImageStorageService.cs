using System.Text.RegularExpressions;

namespace MarketSaaS.Api.Services;

public sealed class LocalImageStorageService : IImageStorageService
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensionPorMime = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    private readonly string _uploadRoot;
    private readonly IWebHostEnvironment _env;

    public LocalImageStorageService(IWebHostEnvironment env)
    {
        _env = env;
        _uploadRoot = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<string> GuardarImagenProductoAsync(
        string negocioId,
        Stream contenido,
        string contentType,
        string nombreOriginal,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(negocioId))
            throw new ArgumentException("NegocioId inválido.");

        var mime = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (!ExtensionPorMime.TryGetValue(mime, out var ext))
            throw new ArgumentException("Formato no permitido. Usá JPG, PNG, WebP o GIF.");

        if (contenido.CanSeek)
        {
            if (contenido.Length > MaxBytes)
                throw new ArgumentException("La imagen supera el máximo de 5 MB.");
        }

        var dirNegocio = Path.Combine(_uploadRoot, SanitizarSegmento(negocioId));
        Directory.CreateDirectory(dirNegocio);

        var nombre = $"{Guid.NewGuid():N}{ext}";
        var ruta = Path.Combine(dirNegocio, nombre);

        await using var fs = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await contenido.CopyToAsync(fs, ct);

        if (fs.Length > MaxBytes)
        {
            fs.Close();
            File.Delete(ruta);
            throw new ArgumentException("La imagen supera el máximo de 5 MB.");
        }

        return $"/uploads/{SanitizarSegmento(negocioId)}/{nombre}";
    }

    private static string SanitizarSegmento(string value) =>
        Regex.Replace(value.Trim(), @"[^a-zA-Z0-9_-]", "");
}
