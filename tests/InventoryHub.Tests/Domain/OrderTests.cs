using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;

namespace InventoryHub.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void AddItem_SameProductTwice_MergesLinesAndCalculatesTotal()
    {
        var product = new Product { Id = 1, Name = "Mouse", UnitPrice = 50m };
        var order = new Order();

        order.AddItem(product, 2);
        order.AddItem(product, 3);

        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
        Assert.Equal(250m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_KeepsPriceAtOrderTime()
    {
        var product = new Product { Id = 1, Name = "Mouse", UnitPrice = 50m };
        var order = new Order();
        order.AddItem(product, 1);

        product.UnitPrice = 80m;

        Assert.Equal(50m, order.Items.First().UnitPrice);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    public void ChangeStatus_AllowedTransition_Succeeds(OrderStatus from, OrderStatus to)
    {
        var order = new Order();
        if (from != OrderStatus.Pending) order.ChangeStatus(from);

        order.ChangeStatus(to);

        Assert.Equal(to, order.Status);
    }

    [Fact]
    public void ChangeStatus_DeliveredOrderCannotBeCancelled()
    {
        var order = new Order();
        order.ChangeStatus(OrderStatus.Confirmed);
        order.ChangeStatus(OrderStatus.Shipped);
        order.ChangeStatus(OrderStatus.Delivered);

        Assert.Throws<DomainException>(() => order.ChangeStatus(OrderStatus.Cancelled));
        Assert.Empty(order.NextStatuses);
    }

    [Fact]
    public void Product_DecreaseStock_MoreThanAvailable_Throws()
    {
        var product = new Product { Name = "SSD" };
        product.IncreaseStock(3);

        Assert.Throws<DomainException>(() => product.DecreaseStock(4));
        Assert.Equal(3, product.StockQuantity);
    }

    [Fact]
    public void Product_SetStock_ReturnsDifference()
    {
        var product = new Product { Name = "SSD" };
        product.IncreaseStock(10);

        var diff = product.SetStock(7);

        Assert.Equal(-3, diff);
        Assert.Equal(7, product.StockQuantity);
    }
}
