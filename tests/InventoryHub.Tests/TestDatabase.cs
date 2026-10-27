using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Tests;

/// <summary>
/// Her test için izole, bellek içi SQLite veritabanı. EF InMemory sağlayıcısının aksine
/// ilişkisel kısıtları (unique index, FK, check constraint) gerçekten uygular.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);

    public async Task<(Category Category, Product Product, Customer Customer)> SeedBasicAsync(int stock = 10)
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Elektronik" };
        var product = new Product { Name = "Klavye", Sku = "KB-1", UnitPrice = 100m, ReorderLevel = 2, Category = category };
        product.IncreaseStock(stock);
        var customer = new Customer { FullName = "Test A.Ş.", Email = "test@example.com" };
        db.AddRange(category, product, customer);
        await db.SaveChangesAsync();
        return (category, product, customer);
    }

    public void Dispose() => _connection.Dispose();
}

public class FakeCurrentUser : ICurrentUserService
{
    public string? UserName => "test@inventoryhub.local";
}
