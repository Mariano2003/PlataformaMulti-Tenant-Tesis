using MarketSaaS.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Infrastructure;

/// <summary>Crea índices al iniciar la API.</summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<MongoIndexInitializer> _log;

    public MongoIndexInitializer(IServiceProvider sp, ILogger<MongoIndexInitializer> log)
    {
        _sp = sp;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InicializarAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudieron crear índices MongoDB al iniciar; la API sigue en ejecución.");
        }
    }

    private async Task InicializarAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        var negocios = db.GetCollection<Negocio>(CollectionNames.Negocios);
        await negocios.Indexes.CreateOneAsync(
            new CreateIndexModel<Negocio>(
                Builders<Negocio>.IndexKeys.Ascending(n => n.Slug),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var usuarios = db.GetCollection<Usuario>(CollectionNames.Usuarios);
        await usuarios.Indexes.CreateOneAsync(
            new CreateIndexModel<Usuario>(
                Builders<Usuario>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var categorias = db.GetCollection<Categoria>(CollectionNames.Categorias);
        await categorias.Indexes.CreateOneAsync(
            new CreateIndexModel<Categoria>(
                Builders<Categoria>.IndexKeys.Ascending(c => c.NegocioId).Ascending(c => c.Nombre),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var productos = db.GetCollection<Producto>(CollectionNames.Productos);
        await productos.Indexes.CreateOneAsync(
            new CreateIndexModel<Producto>(
                Builders<Producto>.IndexKeys.Ascending(p => p.NegocioId).Ascending(p => p.CategoriaId)),
            cancellationToken: cancellationToken);

        // Legado: BSON con clave "ImagenUrl" no mapea a la propiedad [BsonElement("imagenUrl")].
        try
        {
            var productosBson = db.GetCollection<BsonDocument>(CollectionNames.Productos);
            var soloLegacyImagen = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("ImagenUrl", true),
                Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Exists("imagenUrl", true)));
            var renombrados = await productosBson.UpdateManyAsync(
                soloLegacyImagen,
                Builders<BsonDocument>.Update.Rename("ImagenUrl", "imagenUrl"),
                cancellationToken: cancellationToken);
            if (renombrados.ModifiedCount > 0)
                _log.LogInformation(
                    "Productos: migrados {N} documentos (campo BSON ImagenUrl → imagenUrl).",
                    renombrados.ModifiedCount);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Migración de campo imagen en productos omitida.");
        }

        try
        {
            await MigrarLineasPedidosAsync(db, _log, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Migración de líneas en pedidos omitida.");
        }

        var nombresColecciones = await (await db.ListCollectionNamesAsync(cancellationToken: cancellationToken))
            .ToListAsync(cancellationToken);
        if (!nombresColecciones.Contains(CollectionNames.Pedidos))
            await db.CreateCollectionAsync(CollectionNames.Pedidos, cancellationToken: cancellationToken);

        var pedidos = db.GetCollection<Pedido>(CollectionNames.Pedidos);
        await pedidos.Indexes.CreateOneAsync(
            new CreateIndexModel<Pedido>(
                Builders<Pedido>.IndexKeys.Ascending(p => p.NegocioId).Descending(p => p.CreadoEn)),
            cancellationToken: cancellationToken);
        await pedidos.Indexes.CreateOneAsync(
            new CreateIndexModel<Pedido>(
                Builders<Pedido>.IndexKeys.Ascending(p => p.ClienteEmail).Descending(p => p.CreadoEn)),
            cancellationToken: cancellationToken);

        if (!nombresColecciones.Contains(CollectionNames.ChatMensajes))
            await db.CreateCollectionAsync(CollectionNames.ChatMensajes, cancellationToken: cancellationToken);

        var chatMensajes = db.GetCollection<ChatMensaje>(CollectionNames.ChatMensajes);
        await chatMensajes.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMensaje>(
                Builders<ChatMensaje>.IndexKeys.Ascending(m => m.NegocioId).Descending(m => m.EnviadoEn)),
            cancellationToken: cancellationToken);

        if (!nombresColecciones.Contains(CollectionNames.PasswordResetTokens))
            await db.CreateCollectionAsync(CollectionNames.PasswordResetTokens, cancellationToken: cancellationToken);

        var resetTokens = db.GetCollection<PasswordResetToken>(CollectionNames.PasswordResetTokens);
        await resetTokens.Indexes.CreateOneAsync(
            new CreateIndexModel<PasswordResetToken>(
                Builders<PasswordResetToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        if (!nombresColecciones.Contains(CollectionNames.MercadoPagoOAuthStates))
            await db.CreateCollectionAsync(CollectionNames.MercadoPagoOAuthStates, cancellationToken: cancellationToken);

        var mpOAuth = db.GetCollection<MercadoPagoOAuthState>(CollectionNames.MercadoPagoOAuthStates);
        await mpOAuth.Indexes.CreateOneAsync(
            new CreateIndexModel<MercadoPagoOAuthState>(
                Builders<MercadoPagoOAuthState>.IndexKeys.Ascending(s => s.State),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
        await mpOAuth.Indexes.CreateOneAsync(
            new CreateIndexModel<MercadoPagoOAuthState>(
                Builders<MercadoPagoOAuthState>.IndexKeys.Ascending(s => s.ExpiraEn),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }),
            cancellationToken: cancellationToken);

        _log.LogInformation("Índices MongoDB verificados/creados.");
    }

    private static async Task MigrarLineasPedidosAsync(
        IMongoDatabase db,
        ILogger log,
        CancellationToken cancellationToken)
    {
        var col = db.GetCollection<BsonDocument>(CollectionNames.Pedidos);

        var renombrados = await col.UpdateManyAsync(
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("Lineas", true),
                Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Exists("lineas", true))),
            Builders<BsonDocument>.Update.Rename("Lineas", "lineas"),
            cancellationToken: cancellationToken);
        if (renombrados.ModifiedCount > 0)
            log.LogInformation(
                "Pedidos: migrados {N} documentos (campo BSON Lineas → lineas).",
                renombrados.ModifiedCount);

        var cursor = await col
            .Find(Builders<BsonDocument>.Filter.Exists("lineas", true))
            .ToCursorAsync(cancellationToken);

        var actualizados = 0;
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var doc in cursor.Current)
            {
                if (!doc.TryGetValue("lineas", out var lineasVal) || !lineasVal.IsBsonArray)
                    continue;

                var arr = lineasVal.AsBsonArray;
                var nuevo = new BsonArray();
                var changed = false;

                foreach (var el in arr)
                {
                    if (!el.IsBsonDocument)
                    {
                        nuevo.Add(el);
                        continue;
                    }

                    var src = el.AsBsonDocument;
                    var dst = NormalizarLineaBson(src, ref changed);
                    nuevo.Add(dst);
                }

                if (!changed)
                    continue;

                await col.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]),
                    Builders<BsonDocument>.Update.Set("lineas", nuevo),
                    cancellationToken: cancellationToken);
                actualizados++;
            }
        }

        if (actualizados > 0)
            log.LogInformation(
                "Pedidos: normalizadas líneas anidadas en {N} documentos (PascalCase → camelCase).",
                actualizados);
    }

    private static BsonDocument NormalizarLineaBson(BsonDocument src, ref bool changed)
    {
        var dst = new BsonDocument();
        changed |= CopiarCampoLinea(src, dst, "productoId", "ProductoId");
        changed |= CopiarCampoLinea(src, dst, "nombre", "Nombre");
        changed |= CopiarCampoLinea(src, dst, "cantidad", "Cantidad");
        changed |= CopiarCampoLinea(src, dst, "precioUnitario", "PrecioUnitario");
        changed |= CopiarCampoLinea(src, dst, "subtotal", "Subtotal");

        foreach (var kv in src)
        {
            if (!dst.Contains(kv.Name))
                dst[kv.Name] = kv.Value;
        }

        return dst;
    }

    private static bool CopiarCampoLinea(BsonDocument src, BsonDocument dst, string camel, string pascal)
    {
        if (src.Contains(camel))
        {
            dst[camel] = src[camel];
            return false;
        }

        if (!src.Contains(pascal))
            return false;

        dst[camel] = src[pascal];
        return true;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
