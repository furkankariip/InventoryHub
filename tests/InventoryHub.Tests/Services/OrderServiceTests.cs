using InventoryHub.Application.Orders;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task CreateAsync_DecreasesStock_AndWritesSaleMovement()
    {
        var (_, product, customer) = await _database.SeedBasicAsync(stock: 10);
        await using var db = _database.CreateContext();
        var service = new OrderService(db, new FakeCurrentUser());

        var orderId = await service.CreateAsync(new CreateOrderModel
        {
            CustomerId = customer.Id,
            Lines = [new() { ProductId = product.Id, Quantity = 4 }]
        });

        await using var verify = _database.CreateContext();
        var order = await verify.Orders.Include(o => o.Items).SingleAsync(o => o.Id == orderId);
        Assert.Equal(400m, order.TotalAmount);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(6, (await verify.Products.SingleAsync()).StockQuantity);

        var movement = await verify.StockMovements.SingleAsync(m => m.Type == StockMovementType.Sale);
        Assert.Equal(-4, movement.Quantity);
        Assert.Equal(6, movement.StockAfter);
        Assert.Equal(orderId, movement.OrderId);
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ThrowsAndChangesNothing()
    {
        var (_, product, customer) = await _database.SeedBasicAsync(stock: 2);
        await using var db = _database.CreateContext();
        var service = new OrderService(db, new FakeCurrentUser());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateOrderModel
        {
            CustomerId = customer.Id,
            Lines = [new() { ProductId = product.Id, Quantity = 5 }]
        }));

        await using var verify = _database.CreateContext();
        Assert.Equal(2, (await verify.Products.SingleAsync()).StockQuantity);
        Assert.Empty(await verify.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_DuplicateLines_AreMergedBeforeStockCheck()
    {
        var (_, product, customer) = await _database.SeedBasicAsync(stock: 5);
        await using var db = _database.CreateContext();
        var service = new OrderService(db, new FakeCurrentUser());

        // Tek tek bakıldığında 3 ≤ 5 geçerli görünür ama toplam 6 > 5 olduğundan reddedilmelidir.
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(new CreateOrderModel
        {
            CustomerId = customer.Id,
            Lines = [new() { ProductId = product.Id, Quantity = 3 }, new() { ProductId = product.Id, Quantity = 3 }]
        }));
    }

    [Fact]
    public async Task ChangeStatusAsync_Cancel_RestoresStock()
    {
        var (_, product, customer) = await _database.SeedBasicAsync(stock: 10);
        int orderId;
        await using (var db = _database.CreateContext())
        {
            orderId = await new OrderService(db, new FakeCurrentUser()).CreateAsync(new CreateOrderModel
            {
                CustomerId = customer.Id,
                Lines = [new() { ProductId = product.Id, Quantity = 7 }]
            });
        }

        await using (var db = _database.CreateContext())
        {
            await new OrderService(db, new FakeCurrentUser()).ChangeStatusAsync(orderId, OrderStatus.Cancelled);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(10, (await verify.Products.SingleAsync()).StockQuantity);
        Assert.Equal(OrderStatus.Cancelled, (await verify.Orders.SingleAsync()).Status);
        Assert.Single(await verify.StockMovements.Where(m => m.Type == StockMovementType.Return).ToListAsync());
    }

    public void Dispose() => _database.Dispose();
}
