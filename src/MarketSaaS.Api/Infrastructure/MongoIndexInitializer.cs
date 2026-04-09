using MarketSaaS.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        _log.LogInformation("Índices MongoDB verificados/creados.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
