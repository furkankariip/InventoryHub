using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductListItem>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDetail> GetDetailAsync(int id, CancellationToken ct = default);
    Task<ProductFormModel> GetForEditAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderableProduct>> GetOrderableAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<int> CreateAsync(ProductFormModel model, CancellationToken ct = default);
    Task UpdateAsync(ProductFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class ProductService(IApplicationDbContext db, ICurrentUserService currentUser) : IProductService
{
    public async Task<PagedResult<ProductListItem>> GetPagedAsync(ProductQuery query, CancellationToken ct = default)
    {
        var products = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        if (query.CategoryId is > 0)
            products = products.Where(p => p.CategoryId == query.CategoryId);

        if (query.LowStockOnly)
            products = products.Where(p => p.StockQuantity <= p.ReorderLevel);

        products = query.Sort switch
        {
            ProductSort.NameDesc => products.OrderByDescending(p => p.Name),
            ProductSort.Price => products.OrderBy(p => p.UnitPrice),
            ProductSort.PriceDesc => products.OrderByDescending(p => p.UnitPrice),
            ProductSort.Stock => products.OrderBy(p => p.StockQuantity),
            ProductSort.StockDesc => products.OrderByDescending(p => p.StockQuantity),
            ProductSort.Newest => products.OrderByDescending(p => p.CreatedAt),
            _ => products.OrderBy(p => p.Name)
        };

        return await products
            .Select(p => new ProductListItem(
                p.Id, p.Name, p.Sku, p.Category!.Name, p.UnitPrice, p.StockQuantity, p.ReorderLevel, p.IsActive))
            .ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public async Task<ProductDetail> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var product = await db.Products
                          .AsNoTracking()
                          .Include(p => p.Category)
                          .FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException(nameof(Product), id);

        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.ProductId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .Select(m => new ProductStockMovementItem(
                m.CreatedAt, m.Type, m.Quantity, m.StockAfter, m.Description, m.CreatedBy))
            .ToListAsync(ct);

        var totalSold = await db.OrderItems
            .Where(i => i.ProductId == id && i.Order!.Status != OrderStatus.Cancelled)
            .SumAsync(i => (int?)i.Quantity, ct) ?? 0;

        return new ProductDetail(
            product.Id, product.Name, product.Sku, product.Description, product.Category!.Name,
            product.UnitPrice, product.StockQuantity, product.ReorderLevel, product.IsActive,
            product.CreatedAt, totalSold, movements);
    }

    public async Task<ProductFormModel> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException(nameof(Product), id);

        return new ProductFormModel
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            Description = p.Description,
            UnitPrice = p.UnitPrice,
            ReorderLevel = p.ReorderLevel,
            CategoryId = p.CategoryId,
            IsActive = p.IsActive,
            ConcurrencyStamp = p.ConcurrencyStamp
        };
    }

    public async Task<IReadOnlyList<OrderableProduct>> GetOrderableAsync(CancellationToken ct = default) =>
        await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.StockQuantity > 0)
            .OrderBy(p => p.Name)
            .Select(p => new OrderableProduct(p.Id, p.Name, p.Sku, p.UnitPrice, p.StockQuantity))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default) =>
        await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new LookupItem(p.Id, p.Name + " (" + p.Sku + ")"))
            .ToListAsync(ct);

    public async Task<int> CreateAsync(ProductFormModel model, CancellationToken ct = default)
    {
        var sku = NormalizeSku(model.Sku);
        await EnsureUniqueSkuAsync(sku, null, ct);
        await EnsureCategoryExistsAsync(model.CategoryId, ct);

        var product = new Product
        {
            Name = model.Name.Trim(),
            Sku = sku,
            Description = model.Description?.Trim(),
            UnitPrice = model.UnitPrice,
            ReorderLevel = model.ReorderLevel,
            CategoryId = model.CategoryId,
            IsActive = model.IsActive
        };

        if (model.InitialStock > 0)
        {
            product.IncreaseStock(model.InitialStock);
            product.StockMovements.Add(new StockMovement
            {
                Type = StockMovementType.Purchase,
                Quantity = model.InitialStock,
                StockAfter = product.StockQuantity,
                Description = "Açılış stoğu",
                CreatedBy = currentUser.UserName
            });
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task UpdateAsync(ProductFormModel model, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([model.Id], ct)
                      ?? throw new NotFoundException(nameof(Product), model.Id);

        var sku = NormalizeSku(model.Sku);
        await EnsureUniqueSkuAsync(sku, model.Id, ct);
        await EnsureCategoryExistsAsync(model.CategoryId, ct);

        // Formun yüklendiği andaki damga ile karşılaştırılır; arada başka biri güncellediyse
        // SaveChanges DbUpdateConcurrencyException fırlatır.
        db.Products.Entry(product).Property(p => p.ConcurrencyStamp).OriginalValue = model.ConcurrencyStamp;

        product.Name = model.Name.Trim();
        product.Sku = sku;
        product.Description = model.Description?.Trim();
        product.UnitPrice = model.UnitPrice;
        product.ReorderLevel = model.ReorderLevel;
        product.CategoryId = model.CategoryId;
        product.IsActive = model.IsActive;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(
                "Bu ürün siz düzenlerken başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyip tekrar deneyin.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct)
                      ?? throw new NotFoundException(nameof(Product), id);

        db.Products.Remove(product); // DbContext bunu soft delete'e çevirir.
        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    private async Task EnsureUniqueSkuAsync(string sku, int? excludeId, CancellationToken ct)
    {
        // Silinmiş ürünler de SKU'yu tutmaya devam eder; bu yüzden filtre devre dışı bırakılır.
        var exists = await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Sku == sku && p.Id != excludeId, ct);
        if (exists)
            throw new DomainException($"'{sku}' stok kodu başka bir üründe kullanılıyor.");
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw new DomainException("Seçilen kategori bulunamadı.");
    }
}
