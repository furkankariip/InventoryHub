using InventoryHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Common;

/// <summary>
/// Application katmanının veritabanına erişim sözleşmesi. Somut DbContext Infrastructure
/// katmanındadır; böylece iş kuralları altyapıdan bağımsız kalır ve kolayca test edilir.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<StockMovement> StockMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
