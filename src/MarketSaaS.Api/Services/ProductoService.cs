using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class ProductoService : IProductoService
{
    private readonly IMongoCollection<Producto> _productos;
    private readonly IMongoCollection<Categoria> _categorias;

    public ProductoService(IMongoDatabase db)
    {
        _productos = db.GetCollection<Producto>(CollectionNames.Productos);
        _categorias = db.GetCollection<Categoria>(CollectionNames.Categorias);
    }

    public async Task<IReadOnlyList<Producto>> ListarPorNegocioAsync(
        string negocioId,
        bool soloActivos,
        string? categoriaId,
        CancellationToken ct = default)
    {
        var f = Builders<Producto>.Filter.Eq(p => p.NegocioId, negocioId);
        if (soloActivos)
            f &= Builders<Producto>.Filter.Eq(p => p.Activo, true);
        if (!string.IsNullOrWhiteSpace(categoriaId))
            f &= Builders<Producto>.Filter.Eq(p => p.CategoriaId, categoriaId);

        return await _productos.Find(f).SortBy(p => p.Nombre).ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Producto> Items, long Total)> ListarPorNegocioPaginadoAsync(
        string negocioId,
        bool soloActivos,
        string? categoriaId,
        int pagina,
        int tamano,
        CancellationToken ct = default)
    {
        var f = Builders<Producto>.Filter.Eq(p => p.NegocioId, negocioId);
        if (soloActivos)
            f &= Builders<Producto>.Filter.Eq(p => p.Activo, true);
        if (!string.IsNullOrWhiteSpace(categoriaId))
            f &= Builders<Producto>.Filter.Eq(p => p.CategoriaId, categoriaId);

        var (p, t, skip) = PaginacionConsulta.Normalizar(pagina, tamano);
        var total = await _productos.CountDocumentsAsync(f, cancellationToken: ct);
        var items = await _productos
            .Find(f)
            .SortBy(prod => prod.Nombre)
            .Skip(skip)
            .Limit(t)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Producto?> ObtenerPorIdYNegocioAsync(string id, string negocioId, bool soloActivos, CancellationToken ct = default)
    {
        var p = await _productos.Find(x => x.Id == id && x.NegocioId == negocioId).FirstOrDefaultAsync(ct);
        if (p is null)
            return null;
        if (soloActivos && !p.Activo)
            return null;
        return p;
    }

    public async Task<Producto> CrearAsync(string negocioId, CrearProductoRequest dto, CancellationToken ct = default)
    {
        ValidarPrecioStock(dto.Precio, dto.Stock);
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrEmpty(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        await AsegurarCategoriaDelNegocioAsync(negocioId, dto.CategoriaId, ct);
        var imagenUrl = NormalizarImagenUrl(dto.ImagenUrl);

        var prod = new Producto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NegocioId = negocioId,
            CategoriaId = string.IsNullOrWhiteSpace(dto.CategoriaId) ? null : dto.CategoriaId.Trim(),
            Nombre = nombre,
            DescripcionCorta = dto.DescripcionCorta?.Trim(),
            ImagenUrl = imagenUrl,
            Precio = dto.Precio,
            Stock = dto.Stock,
            Atributos = NormalizarAtributos(dto.Atributos),
            Activo = true,
            CreadoEn = DateTime.UtcNow,
        };

        await _productos.InsertOneAsync(prod, cancellationToken: ct);
        return prod;
    }

    public async Task<Producto?> ActualizarAsync(string negocioId, string id, ActualizarProductoRequest dto, CancellationToken ct = default)
    {
        ValidarPrecioStock(dto.Precio, dto.Stock);
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrEmpty(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        var actual = await _productos.Find(p => p.Id == id && p.NegocioId == negocioId).FirstOrDefaultAsync(ct);
        if (actual is null)
            return null;

        await AsegurarCategoriaDelNegocioAsync(negocioId, dto.CategoriaId, ct);
        var imagenUrl = NormalizarImagenUrl(dto.ImagenUrl);

        actual.CategoriaId = string.IsNullOrWhiteSpace(dto.CategoriaId) ? null : dto.CategoriaId.Trim();
        actual.Nombre = nombre;
        actual.DescripcionCorta = dto.DescripcionCorta?.Trim();
        actual.ImagenUrl = imagenUrl;
        actual.Precio = dto.Precio;
        actual.Stock = dto.Stock;
        actual.Atributos = NormalizarAtributos(dto.Atributos);
        actual.Activo = dto.Activo;

        await _productos.ReplaceOneAsync(p => p.Id == id && p.NegocioId == negocioId, actual, cancellationToken: ct);
        return actual;
    }

    public async Task<bool> EliminarAsync(string negocioId, string id, CancellationToken ct = default)
    {
        var res = await _productos.DeleteOneAsync(p => p.Id == id && p.NegocioId == negocioId, ct);
        return res.DeletedCount > 0;
    }

    private async Task AsegurarCategoriaDelNegocioAsync(string negocioId, string? categoriaId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoriaId))
            return;
        var cid = categoriaId.Trim();
        if (!ObjectId.TryParse(cid, out _))
            throw new ArgumentException("CategoriaId no es un ObjectId válido.");
        var cat = await _categorias.Find(c => c.Id == cid && c.NegocioId == negocioId).FirstOrDefaultAsync(ct);
        if (cat is null)
            throw new InvalidOperationException("La categoría no existe o no pertenece a este negocio.");
    }

    private static void ValidarPrecioStock(decimal precio, int stock)
    {
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.");
        if (stock < 0)
            throw new ArgumentException("El stock no puede ser negativo.");
    }

    private static string? NormalizarImagenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var t = url.Trim().Trim('\'', '"', '\u200b', '\u200c', '\u200d', '\ufeff');
        if (string.IsNullOrEmpty(t))
            return null;
        if (t.Length > 2048)
            throw new ArgumentException("ImagenUrl supera el largo máximo permitido.");
        if (t.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("ImagenUrl no permitida.");
        return t;
    }

    private static Dictionary<string, string>? NormalizarAtributos(Dictionary<string, string>? atributos)
    {
        if (atributos is null || atributos.Count == 0)
            return null;
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in atributos)
        {
            var k = kv.Key?.Trim();
            if (string.IsNullOrEmpty(k))
                continue;
            d[k] = kv.Value?.Trim() ?? "";
        }

        return d.Count == 0 ? null : d;
    }
}
