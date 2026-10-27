using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Stock;

public interface IStockService
{
    Task AdjustAsync(StockAdjustmentModel model, CancellationToken ct = default);
    Task<PagedResult<StockMovementListItem>> GetMovementsAsync(StockMovementQuery query, CancellationToken ct = default);
}

public class StockService(IApplicationDbContext db, ICurrentUserService currentUser) : IStockService
{
    public async Task AdjustAsync(StockAdjustmentModel model, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([model.ProductId], ct)
                      ?? throw new NotFoundException(nameof(Product), model.ProductId);

        int change;
        switch (model.Type)
        {
            case StockMovementType.Purchase:
            case StockMovementType.Return:
                product.IncreaseStock(model.Quantity);
                change = model.Quantity;
                break;
            case StockMovementType.Adjustment:
                change = product.SetStock(model.Quantity);
                if (change == 0)
                    throw new DomainException("Yeni stok miktarı mevcut stokla aynı; değişiklik yapılmadı.");
                break;
            default:
                throw new DomainException("Satış hareketleri yalnızca sipariş üzerinden oluşturulabilir.");
        }

        db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Type = model.Type,
            Quantity = change,
            StockAfter = product.StockQuantity,
            Description = model.Description?.Trim(),
            CreatedBy = currentUser.UserName
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<StockMovementListItem>> GetMovementsAsync(
        StockMovementQuery query, CancellationToken ct = default)
    {
        // Silinmiş ürünlerin geçmiş hareketleri de görünmeli.
        var movements = db.StockMovements.IgnoreQueryFilters().AsNoTracking();

        if (query.ProductId is > 0)
            movements = movements.Where(m => m.ProductId == query.ProductId);
        if (query.Type.HasValue)
            movements = movements.Where(m => m.Type == query.Type);

        return await movements
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Select(m => new StockMovementListItem(
                m.Id, m.CreatedAt, m.ProductId, m.Product!.Name, m.Product.Sku, m.Type,
                m.Quantity, m.StockAfter, m.Description, m.CreatedBy, m.OrderId))
            .ToPagedResultAsync(query.Page, query.PageSize, ct);
    }
}
