using InventoryHub.Domain.Common;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;

namespace InventoryHub.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public void AddItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Sipariş miktarı sıfırdan büyük olmalıdır.");

        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = quantity,
                UnitPrice = product.UnitPrice
            });
        }

        RecalculateTotal();
    }

    public void RecalculateTotal() => TotalAmount = Items.Sum(i => i.LineTotal);

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public IReadOnlyCollection<OrderStatus> NextStatuses => AllowedTransitions[Status];

    public bool CanTransitionTo(OrderStatus next) => AllowedTransitions[Status].Contains(next);

    public void ChangeStatus(OrderStatus next)
    {
        if (!CanTransitionTo(next))
            throw new DomainException($"Sipariş '{Status}' durumundayken '{next}' durumuna geçirilemez.");
        Status = next;
    }
}
