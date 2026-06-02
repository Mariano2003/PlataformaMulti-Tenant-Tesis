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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
