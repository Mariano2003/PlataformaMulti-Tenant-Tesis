using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class CategoriaService : ICategoriaService
{
    private readonly IMongoCollection<Categoria> _categorias;
    private readonly IMongoCollection<Producto> _productos;

    public CategoriaService(IMongoDatabase db)
    {
        _categorias = db.GetCollection<Categoria>(CollectionNames.Categorias);
        _productos = db.GetCollection<Producto>(CollectionNames.Productos);
    }

    public async Task<IReadOnlyList<Categoria>> ListarPorNegocioAsync(string negocioId, bool soloActivos, CancellationToken ct = default)
    {
        var f = Builders<Categoria>.Filter.Eq(c => c.NegocioId, negocioId);
        if (soloActivos)
            f &= Builders<Categoria>.Filter.Eq(c => c.Activo, true);

        return await _categorias
            .Find(f)
            .Sort(Builders<Categoria>.Sort.Ascending(c => c.Orden).Ascending(c => c.Nombre))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Categoria> Items, long Total)> ListarPorNegocioPaginadoAsync(
        string negocioId,
        bool soloActivos,
        int pagina,
        int tamano,
        CancellationToken ct = default)
    {
        var f = Builders<Categoria>.Filter.Eq(c => c.NegocioId, negocioId);
        if (soloActivos)
            f &= Builders<Categoria>.Filter.Eq(c => c.Activo, true);

        var (p, t, skip) = PaginacionConsulta.Normalizar(pagina, tamano);
        var sort = Builders<Categoria>.Sort.Ascending(c => c.Orden).Ascending(c => c.Nombre);
        var total = await _categorias.CountDocumentsAsync(f, cancellationToken: ct);
        var items = await _categorias
            .Find(f)
            .Sort(sort)
            .Skip(skip)
            .Limit(t)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Categoria?> ObtenerPorIdYNegocioAsync(string id, string negocioId, CancellationToken ct = default)
    {
        Categoria? found = await _categorias.Find(c => c.Id == id && c.NegocioId == negocioId).FirstOrDefaultAsync(ct);
        return found;
    }

    public async Task<Categoria> CrearAsync(string negocioId, CrearCategoriaRequest dto, CancellationToken ct = default)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrEmpty(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        var duplicado = await _categorias
            .Find(c => c.NegocioId == negocioId && c.Nombre == nombre)
            .AnyAsync(ct);
        if (duplicado)
            throw new InvalidOperationException($"Ya existe la categoría '{nombre}' en este negocio.");

        var cat = new Categoria
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NegocioId = negocioId,
            Nombre = nombre,
            Orden = dto.Orden ?? 0,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
        };

        await _categorias.InsertOneAsync(cat, cancellationToken: ct);
        return cat;
    }

    public async Task<Categoria?> ActualizarAsync(string negocioId, string id, ActualizarCategoriaRequest dto, CancellationToken ct = default)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrEmpty(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        var actual = await ObtenerPorIdYNegocioAsync(id, negocioId, ct);
        if (actual is null)
            return null;

        var otroIgual = await _categorias
            .Find(c => c.NegocioId == negocioId && c.Nombre == nombre && c.Id != id)
            .AnyAsync(ct);
        if (otroIgual)
            throw new InvalidOperationException($"Ya existe otra categoría con el nombre '{nombre}'.");

        actual.Nombre = nombre;
        actual.Orden = dto.Orden;
        actual.Activo = dto.Activo;

        await _categorias.ReplaceOneAsync(c => c.Id == id && c.NegocioId == negocioId, actual, cancellationToken: ct);
        return actual;
    }

    public async Task<bool> EliminarAsync(string negocioId, string id, CancellationToken ct = default)
    {
        var actual = await ObtenerPorIdYNegocioAsync(id, negocioId, ct);
        if (actual is null)
            return false;

        var hayProductos = await _productos
            .Find(p => p.NegocioId == negocioId && p.CategoriaId == id)
            .AnyAsync(ct);
        if (hayProductos)
            throw new InvalidOperationException("No se puede eliminar: hay productos asociados a esta categoría.");

        var res = await _categorias.DeleteOneAsync(c => c.Id == id && c.NegocioId == negocioId, ct);
        return res.DeletedCount > 0;
    }
}
