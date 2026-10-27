using InventoryHub.Application.Products;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task CreateAsync_DuplicateSku_IsRejected_CaseInsensitive()
    {
        var (category, _, _) = await _database.SeedBasicAsync();
        await using var db = _database.CreateContext();
        var service = new ProductService(db, new FakeCurrentUser());

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new ProductFormModel
        {
            Name = "Başka Klavye", Sku = "kb-1", UnitPrice = 10m, CategoryId = category.Id
        }));
        Assert.Contains("KB-1", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithInitialStock_WritesOpeningMovement()
    {
        var (category, _, _) = await _database.SeedBasicAsync();
        await using var db = _database.CreateContext();
        var service = new ProductService(db, new FakeCurrentUser());

        var id = await service.CreateAsync(new ProductFormModel
        {
            Name = "Mouse", Sku = "ms-1", UnitPrice = 25m, CategoryId = category.Id, InitialStock = 30
        });

        await using var verify = _database.CreateContext();
        var product = await verify.Products.SingleAsync(p => p.Id == id);
        Assert.Equal("MS-1", product.Sku);
        Assert.Equal(30, product.StockQuantity);
        Assert.Equal(30, (await verify.StockMovements.SingleAsync(m => m.ProductId == id)).Quantity);
    }

    [Fact]
    public async Task DeleteAsync_IsSoftDelete_AndHiddenFromQueries()
    {
        var (_, product, _) = await _database.SeedBasicAsync();
        await using (var db = _database.CreateContext())
        {
            await new ProductService(db, new FakeCurrentUser()).DeleteAsync(product.Id);
        }

        await using var verify = _database.CreateContext();
        Assert.False(await verify.Products.AnyAsync());

        var deleted = await verify.Products.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleConcurrencyStamp_Throws()
    {
        var (_, product, _) = await _database.SeedBasicAsync();

        ProductFormModel staleForm;
        await using (var db = _database.CreateContext())
        {
            var service = new ProductService(db, new FakeCurrentUser());
            staleForm = await service.GetForEditAsync(product.Id);

            // Başka bir kullanıcı aynı ürünü önce güncelliyor.
            var otherForm = await service.GetForEditAsync(product.Id);
            otherForm.UnitPrice = 150m;
            await service.UpdateAsync(otherForm);
        }

        await using var db2 = _database.CreateContext();
        staleForm.UnitPrice = 120m;
        await Assert.ThrowsAsync<DomainException>(() =>
            new ProductService(db2, new FakeCurrentUser()).UpdateAsync(staleForm));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearchAndLowStock()
    {
        var (category, _, _) = await _database.SeedBasicAsync(stock: 1); // ReorderLevel = 2 → kritik
        await using var db = _database.CreateContext();
        var service = new ProductService(db, new FakeCurrentUser());
        await service.CreateAsync(new ProductFormModel
        {
            Name = "Monitör", Sku = "MN-1", UnitPrice = 10m, CategoryId = category.Id, InitialStock = 50
        });

        var search = await service.GetPagedAsync(new ProductQuery { Search = "Monit" });
        var lowStock = await service.GetPagedAsync(new ProductQuery { LowStockOnly = true });

        Assert.Equal("Monitör", Assert.Single(search.Items).Name);
        Assert.Equal("Klavye", Assert.Single(lowStock.Items).Name);
    }

    public void Dispose() => _database.Dispose();
}
