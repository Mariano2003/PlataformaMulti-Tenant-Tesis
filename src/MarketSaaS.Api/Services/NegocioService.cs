using System.Collections.Generic;
using System.Text.RegularExpressions;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class NegocioService : INegocioService
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    private readonly IMongoCollection<Negocio> _negocios;

    public NegocioService(IMongoDatabase db) =>
        _negocios = db.GetCollection<Negocio>(CollectionNames.Negocios);

    public async Task<Negocio?> ObtenerPorIdAsync(string id, CancellationToken ct = default)
    {
        return await _negocios.Find(n => n.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<Negocio?> ObtenerPorSlugAsync(string slug, CancellationToken ct = default)
    {
        var s = NormalizarSlug(slug);
        return await _negocios.Find(n => n.Slug == s).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Negocio>> ListarActivosOrdenadosAsync(CancellationToken ct = default)
    {
        var lista = await _negocios.Find(n => n.Activo)
            .SortBy(n => n.Nombre)
            .ToListAsync(ct);
        return lista;
    }

    public async Task<Negocio> CrearAsync(CrearNegocioRequest dto, CancellationToken ct = default)
    {
        var slug = NormalizarSlug(dto.Slug);
        if (!SlugRegex.IsMatch(slug))
            throw new ArgumentException("Slug inválido: usar minúsculas, números y guiones.");

        var existe = await _negocios.Find(n => n.Slug == slug).AnyAsync(ct);
        if (existe)
            throw new InvalidOperationException($"Ya existe un negocio con slug '{slug}'.");

        var negocio = new Negocio
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Slug = slug,
            Nombre = dto.Nombre.Trim(),
            DescripcionCorta = dto.DescripcionCorta?.Trim(),
            LogoUrl = dto.LogoUrl?.Trim(),
            TemaJson = dto.TemaJson,
            EmailContacto = dto.EmailContacto?.Trim(),
            Activo = true,
            CreadoEn = DateTime.UtcNow,
        };

        await _negocios.InsertOneAsync(negocio, cancellationToken: ct);
        return negocio;
    }

    public Task EliminarPorIdAsync(string id, CancellationToken ct = default) =>
        _negocios.DeleteOneAsync(n => n.Id == id, ct);

    public async Task ActualizarMercadoPagoAsync(string negocioId, ActualizarMercadoPagoNegocioRequest dto, CancellationToken ct = default)
    {
        var updates = new List<UpdateDefinition<Negocio>>();
        if (dto.AccessToken is not null)
        {
            var tok = string.IsNullOrWhiteSpace(dto.AccessToken) ? null : dto.AccessToken.Trim();
            updates.Add(Builders<Negocio>.Update.Set(n => n.MercadoPagoAccessToken, tok));
        }

        if (dto.WebhookSecret is not null)
        {
            var sec = string.IsNullOrWhiteSpace(dto.WebhookSecret) ? null : dto.WebhookSecret.Trim();
            updates.Add(Builders<Negocio>.Update.Set(n => n.MercadoPagoWebhookSecret, sec));
        }

        if (updates.Count == 0)
            return;

        await _negocios.UpdateOneAsync(
            n => n.Id == negocioId,
            Builders<Negocio>.Update.Combine(updates),
            cancellationToken: ct);
    }

    private static string NormalizarSlug(string slug) => slug.Trim().ToLowerInvariant();
}
