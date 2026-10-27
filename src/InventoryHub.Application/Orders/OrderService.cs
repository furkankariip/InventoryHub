using System.Security.Cryptography;
using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Orders;

public interface IOrderService
{
    Task<PagedResult<OrderListItem>> GetPagedAsync(OrderQuery query, CancellationToken ct = default);
    Task<OrderDetail> GetDetailAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateOrderModel model, CancellationToken ct = default);
    Task ChangeStatusAsync(int id, OrderStatus newStatus, CancellationToken ct = default);
}

public class OrderService(IApplicationDbContext db, ICurrentUserService currentUser) : IOrderService
{
    public async Task<PagedResult<OrderListItem>> GetPagedAsync(OrderQuery query, CancellationToken ct = default)
    {
        // Silinmiş müşterilerin geçmiş siparişleri de listelenmeli; bu yüzden filtreler kapatılır.
        var orders = db.Orders.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            orders = orders.Where(o => o.OrderNumber.Contains(term) || o.Customer!.FullName.Contains(term));
        }

        if (query.Status.HasValue)
            orders = orders.Where(o => o.Status == query.Status);
        if (query.From.HasValue)
            orders = orders.Where(o => o.OrderDate >= query.From.Value.Date);
        if (query.To.HasValue)
            orders = orders.Where(o => o.OrderDate < query.To.Value.Date.AddDays(1));

        return await orders
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderListItem(
                o.Id, o.OrderNumber, o.OrderDate, o.Customer!.FullName, o.Items.Sum(i => i.Quantity),
                o.TotalAmount, o.Status))
            .ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public async Task<OrderDetail> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var order = await db.Orders
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Include(o => o.Customer)
                        .Include(o => o.Items).ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw new NotFoundException(nameof(Order), id);

        var lines = order.Items
            .Select(i => new OrderLineDetail(i.ProductId, i.Product!.Name, i.Product.Sku, i.Quantity, i.UnitPrice))
            .ToList();

        return new OrderDetail(
            order.Id, order.OrderNumber, order.OrderDate, order.Status, order.NextStatuses,
            order.CustomerId, order.Customer!.FullName, order.Customer.Email, order.Notes, order.CreatedBy,
            order.TotalAmount, lines);
    }

    /// <summary>
    /// Siparişi oluşturur, ürün stoklarını düşer ve her kalem için stok hareketi yazar.
    /// Tüm değişiklikler tek bir SaveChanges çağrısında (tek veritabanı transaction'ı) kaydedilir;
    /// aynı ürüne eşzamanlı sipariş gelirse ConcurrencyStamp sayesinde stok eksiye düşmez.
    /// </summary>
    public async Task<int> CreateAsync(CreateOrderModel model, CancellationToken ct = default)
    {
        var lines = model.Lines
            .Where(l => l.ProductId > 0 && l.Quantity > 0)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToList();

        if (lines.Count == 0)
            throw new DomainException("Siparişe en az bir ürün ekleyin.");

        if (!await db.Customers.AnyAsync(c => c.Id == model.CustomerId, ct))
            throw new DomainException("Seçilen müşteri bulunamadı.");

        var productIds = lines.Select(l => l.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            OrderDate = DateTime.UtcNow,
            CustomerId = model.CustomerId,
            Notes = model.Notes?.Trim(),
            CreatedBy = currentUser.UserName
        };

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new DomainException("Siparişteki ürünlerden biri bulunamadı.");
            if (!product.IsActive)
                throw new DomainException($"'{product.Name}' şu anda satışta değil.");

            product.DecreaseStock(line.Quantity);
            order.AddItem(product, line.Quantity);

            db.StockMovements.Add(new StockMovement
            {
                Product = product,
                Order = order,
                Type = StockMovementType.Sale,
                Quantity = -line.Quantity,
                StockAfter = product.StockQuantity,
                Description = $"Sipariş {order.OrderNumber}",
                CreatedBy = currentUser.UserName
            });
        }

        db.Orders.Add(order);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("Ürün stokları siz siparişi oluştururken değişti. Lütfen tekrar deneyin.");
        }

        return order.Id;
    }

    public async Task ChangeStatusAsync(int id, OrderStatus newStatus, CancellationToken ct = default)
    {
        var order = await db.Orders
                        .Include(o => o.Items).ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw new NotFoundException(nameof(Order), id);

        order.ChangeStatus(newStatus);

        if (newStatus == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                // Ürün sonradan silinmiş olabilir; bu durumda stok iadesi yapılmaz.
                if (item.Product is null)
                    continue;

                item.Product.IncreaseStock(item.Quantity);
                db.StockMovements.Add(new StockMovement
                {
                    Product = item.Product,
                    OrderId = order.Id,
                    Type = StockMovementType.Return,
                    Quantity = item.Quantity,
                    StockAfter = item.Product.StockQuantity,
                    Description = $"Sipariş {order.OrderNumber} iptali",
                    CreatedBy = currentUser.UserName
                });
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin.");
        }
    }

    private static string GenerateOrderNumber() =>
        $"SP-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetHexString(6)}";
}
