using System.Text.RegularExpressions;
using MarketSaaS.Api.DTOs;
using MarketSaaS.Api.Infrastructure;
using MarketSaaS.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MarketSaaS.Api.Services;

public sealed class PedidoService : IPedidoService
{
    private readonly IMongoCollection<Pedido> _pedidos;
    private readonly IMongoCollection<Producto> _productos;
    private readonly IPedidoEmailService _emails;

    public PedidoService(IMongoDatabase db, IPedidoEmailService emails)
    {
        _pedidos = db.GetCollection<Pedido>(CollectionNames.Pedidos);
        _productos = db.GetCollection<Producto>(CollectionNames.Productos);
        _emails = emails;
    }

    public async Task<Pedido> CrearPendienteDePagoAsync(
        string negocioId,
        CrearPedidoRequest solicitud,
        string? clienteUsuarioId = null,
        CancellationToken ct = default)
    {
        if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
            throw new ArgumentException("El pedido debe incluir al menos una línea.");

        var emailCliente = solicitud.ClienteEmail?.Trim();
        if (string.IsNullOrEmpty(emailCliente))
            throw new ArgumentException("ClienteEmail es obligatorio.");

        var (lineasParaPersistir, totalPedido) = await ValidarLineasYCalcularTotalesAsync(negocioId, solicitud, ct);

        var pedido = new Pedido
        {
            Id = ObjectId.GenerateNewId().ToString(),
            NegocioId = negocioId,
            Estado = PedidoEstados.PendientePago,
            Lineas = lineasParaPersistir,
            Total = totalPedido,
            ClienteNombre = solicitud.ClienteNombre?.Trim(),
            ClienteEmail = emailCliente,
            ClienteUsuarioId = string.IsNullOrWhiteSpace(clienteUsuarioId) ? null : clienteUsuarioId.Trim(),
            ClienteTelefono = solicitud.ClienteTelefono?.Trim(),
            CreadoEn = DateTime.UtcNow,
        };

        await _pedidos.InsertOneAsync(pedido, cancellationToken: ct);
        return pedido;
    }

    public async Task<bool> AsociarPreferenciaMercadoPagoAsync(
        string negocioId,
        string pedidoId,
        string preferenceId,
        CancellationToken ct = default)
    {
        var res = await _pedidos.UpdateOneAsync(
            p => p.Id == pedidoId && p.NegocioId == negocioId && p.Estado == PedidoEstados.PendientePago,
            Builders<Pedido>.Update.Set(p => p.MercadoPagoPreferenceId, preferenceId),
            cancellationToken: ct);

        return res.ModifiedCount == 1;
    }

    public async Task ProcesarPagoAprobadoMercadoPagoAsync(string pedidoId, string mercadoPagoPaymentId, CancellationToken ct = default)
    {
        var reserva = await _pedidos.UpdateOneAsync(
            p => p.Id == pedidoId && p.Estado == PedidoEstados.PendientePago,
            Builders<Pedido>.Update
                .Set(p => p.Estado, PedidoEstados.ProcesandoPago)
                .Set(p => p.MercadoPagoPaymentId, mercadoPagoPaymentId),
            cancellationToken: ct);

        if (reserva.ModifiedCount == 0)
        {
            var existente = await _pedidos.Find(p => p.Id == pedidoId).FirstOrDefaultAsync(ct);
            if (existente?.Estado == PedidoEstados.Pagado)
                return;
            return;
        }

        var pedido = await _pedidos.Find(p => p.Id == pedidoId).FirstAsync(ct);
        var lineasAgrupadasPorProducto = pedido.Lineas
            .Select(linea => (ProductoId: linea.ProductoId, Cantidad: linea.Cantidad))
            .ToList();

        var descuentosDeStockAplicados = new List<(string ProductoId, int Cantidad)>();
        try
        {
            foreach (var (productoId, cantidadTotal) in lineasAgrupadasPorProducto)
            {
                var resultadoActualizacionStock = await _productos.UpdateOneAsync(
                    producto => producto.Id == productoId
                        && producto.NegocioId == pedido.NegocioId
                        && producto.Activo
                        && producto.Stock >= cantidadTotal,
                    Builders<Producto>.Update.Inc(producto => producto.Stock, -cantidadTotal),
                    cancellationToken: ct);

                if (resultadoActualizacionStock.ModifiedCount != 1)
                {
                    await RevertirDescuentosDeStockAsync(descuentosDeStockAplicados, ct);
                    await _pedidos.UpdateOneAsync(
                        p => p.Id == pedidoId && p.Estado == PedidoEstados.ProcesandoPago,
                        Builders<Pedido>.Update
                            .Set(p => p.Estado, PedidoEstados.PendientePago)
                            .Unset(p => p.MercadoPagoPaymentId),
                        cancellationToken: ct);
                    throw new InvalidOperationException(
                        "No se pudo descontar stock al confirmar el pago (posible concurrencia). Se dejó el pedido pendiente.");
                }

                descuentosDeStockAplicados.Add((productoId, cantidadTotal));
            }

            await _pedidos.UpdateOneAsync(
                p => p.Id == pedidoId && p.Estado == PedidoEstados.ProcesandoPago,
                Builders<Pedido>.Update.Set(p => p.Estado, PedidoEstados.Pagado),
                cancellationToken: ct);

            pedido.Estado = PedidoEstados.Pagado;
            await _emails.NotificarPagoConfirmadoAsync(pedido, ct);
        }
        catch
        {
            await RevertirDescuentosDeStockAsync(descuentosDeStockAplicados, ct);
            await _pedidos.UpdateOneAsync(
                p => p.Id == pedidoId && p.Estado == PedidoEstados.ProcesandoPago,
                Builders<Pedido>.Update
                    .Set(p => p.Estado, PedidoEstados.PendientePago)
                    .Unset(p => p.MercadoPagoPaymentId),
                cancellationToken: ct);
            throw;
        }
    }

    public async Task MarcarPedidoRechazadoSiPendienteMercadoPagoAsync(
        string pedidoId,
        string mercadoPagoPaymentId,
        string? mercadoPagoStatusDetail = null,
        CancellationToken ct = default)
    {
        var detalle = mercadoPagoStatusDetail?.Trim();
        if (detalle?.Length > 500)
            detalle = detalle[..500];

        var update = Builders<Pedido>.Update
            .Set(p => p.Estado, PedidoEstados.Rechazado)
            .Set(p => p.MercadoPagoPaymentId, mercadoPagoPaymentId);

        if (string.IsNullOrEmpty(detalle))
            update = update.Unset(p => p.MercadoPagoStatusDetail);
        else
            update = update.Set(p => p.MercadoPagoStatusDetail, detalle);

        await _pedidos.UpdateOneAsync(
            p => p.Id == pedidoId && p.Estado == PedidoEstados.PendientePago,
            update,
            cancellationToken: ct);
    }

    private async Task<(List<PedidoLinea> lineas, decimal total)> ValidarLineasYCalcularTotalesAsync(
        string negocioId,
        CrearPedidoRequest solicitud,
        CancellationToken ct)
    {
        var lineasAgrupadasPorProducto = solicitud.Lineas
            .GroupBy(linea => linea.ProductoId.Trim())
            .Select(grupo => (ProductoId: grupo.Key, Cantidad: grupo.Sum(linea => linea.Cantidad)))
            .ToList();

        foreach (var (productoId, cantidadTotal) in lineasAgrupadasPorProducto)
        {
            if (!ObjectId.TryParse(productoId, out _))
                throw new ArgumentException($"ProductoId inválido: {productoId}.");
            if (cantidadTotal < 1)
                throw new ArgumentException("La cantidad debe ser al menos 1 por producto.");
        }

        var idsProductosSolicitados = lineasAgrupadasPorProducto.Select(x => x.ProductoId).Distinct().ToList();
        var productosEncontrados = await _productos
            .Find(producto => idsProductosSolicitados.Contains(producto.Id) && producto.NegocioId == negocioId)
            .ToListAsync(ct);
        if (productosEncontrados.Count != idsProductosSolicitados.Count)
            throw new InvalidOperationException("Uno o más productos no existen en esta tienda.");

        var productosPorId = productosEncontrados.ToDictionary(producto => producto.Id, StringComparer.Ordinal);
        var lineasParaPersistir = new List<PedidoLinea>();
        decimal totalPedido = 0;

        foreach (var (productoId, cantidadTotal) in lineasAgrupadasPorProducto)
        {
            var producto = productosPorId[productoId];
            if (!producto.Activo)
                throw new InvalidOperationException($"El producto '{producto.Nombre}' no está disponible.");
            if (producto.Stock < cantidadTotal)
                throw new InvalidOperationException($"Stock insuficiente para '{producto.Nombre}' (disponible: {producto.Stock}, pedido: {cantidadTotal}).");

            var subtotalLinea = Math.Round(producto.Precio * cantidadTotal, 2, MidpointRounding.AwayFromZero);
            totalPedido += subtotalLinea;
            lineasParaPersistir.Add(new PedidoLinea
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                Cantidad = cantidadTotal,
                PrecioUnitario = producto.Precio,
                Subtotal = subtotalLinea,
            });
        }

        totalPedido = Math.Round(totalPedido, 2, MidpointRounding.AwayFromZero);
        return (lineasParaPersistir, totalPedido);
    }

    private async Task RevertirDescuentosDeStockAsync(
        List<(string ProductoId, int Cantidad)> descuentosDeStockAplicados,
        CancellationToken ct)
    {
        foreach (var (productoId, cantidadDescontada) in descuentosDeStockAplicados)
        {
            await _productos.UpdateOneAsync(
                producto => producto.Id == productoId,
                Builders<Producto>.Update.Inc(producto => producto.Stock, cantidadDescontada),
                cancellationToken: ct);
        }
    }

    public async Task<IReadOnlyList<Pedido>> ListarPorNegocioAsync(string negocioId, int limite, CancellationToken ct = default)
    {
        var cantidadMaxima = Math.Clamp(limite, 1, 500);
        return await _pedidos
            .Find(pedido => pedido.NegocioId == negocioId)
            .SortByDescending(pedido => pedido.CreadoEn)
            .Limit(cantidadMaxima)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Pedido> Items, long Total)> ListarPorNegocioPaginadoAsync(
        string negocioId,
        int pagina,
        int tamano,
        CancellationToken ct = default)
    {
        var (p, t, skip) = PaginacionConsulta.Normalizar(pagina, tamano);
        var filtro = Builders<Pedido>.Filter.Eq(pedido => pedido.NegocioId, negocioId);
        var total = await _pedidos.CountDocumentsAsync(filtro, cancellationToken: ct);
        var items = await _pedidos
            .Find(filtro)
            .SortByDescending(pedido => pedido.CreadoEn)
            .Skip(skip)
            .Limit(t)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Pedido>> ListarPorClienteAsync(
        string? clienteEmail,
        string? clienteUsuarioId,
        int limite,
        CancellationToken ct = default)
    {
        var cantidadMaxima = Math.Clamp(limite, 1, 200);
        var filtros = new List<FilterDefinition<Pedido>>();

        var emailNorm = clienteEmail?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(emailNorm))
        {
            filtros.Add(Builders<Pedido>.Filter.Regex(
                p => p.ClienteEmail,
                new BsonRegularExpression($"^{Regex.Escape(emailNorm)}$", "i")));
        }

        var usuarioId = clienteUsuarioId?.Trim();
        if (!string.IsNullOrEmpty(usuarioId))
            filtros.Add(Builders<Pedido>.Filter.Eq(p => p.ClienteUsuarioId, usuarioId));

        if (filtros.Count == 0)
            return Array.Empty<Pedido>();

        var filtro = filtros.Count == 1 ? filtros[0] : Builders<Pedido>.Filter.Or(filtros);

        return await _pedidos
            .Find(filtro)
            .SortByDescending(p => p.CreadoEn)
            .Limit(cantidadMaxima)
            .ToListAsync(ct);
    }

    public async Task<Pedido?> ObtenerPorIdYNegocioAsync(string id, string negocioId, CancellationToken ct = default)
    {
        Pedido? pedido = await _pedidos.Find(p => p.Id == id && p.NegocioId == negocioId).FirstOrDefaultAsync(ct);
        return pedido;
    }

    public async Task<Pedido?> ActualizarEstadoAdminAsync(
        string negocioId,
        string pedidoId,
        string nuevoEstado,
        CancellationToken ct = default)
    {
        var estado = nuevoEstado.Trim();
        if (!PedidoEstados.EstadosGestionAdmin.Contains(estado))
            throw new ArgumentException($"Estado no permitido: {estado}.");

        var pedido = await ObtenerPorIdYNegocioAsync(pedidoId, negocioId, ct);
        if (pedido is null)
            return null;

        if (!PedidoEstados.AdminPuedeGestionar(pedido.Estado))
            throw new InvalidOperationException($"No se puede cambiar el estado desde «{pedido.Estado}».");

        if (pedido.Estado == estado)
            return pedido;

        await _pedidos.UpdateOneAsync(
            p => p.Id == pedidoId && p.NegocioId == negocioId,
            Builders<Pedido>.Update.Set(p => p.Estado, estado),
            cancellationToken: ct);

        pedido.Estado = estado;
        await _emails.NotificarCambioEstadoAsync(pedido, ct);
        return pedido;
    }

    public async Task<int> ContarPedidosPagadosDesdeAsync(string negocioId, DateTime desdeUtc, CancellationToken ct = default)
    {
        var filtro = Builders<Pedido>.Filter.And(
            Builders<Pedido>.Filter.Eq(p => p.NegocioId, negocioId),
            Builders<Pedido>.Filter.Eq(p => p.Estado, PedidoEstados.Pagado),
            Builders<Pedido>.Filter.Gt(p => p.CreadoEn, desdeUtc));

        return (int)await _pedidos.CountDocumentsAsync(filtro, cancellationToken: ct);
    }
}
