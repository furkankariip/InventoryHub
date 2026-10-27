using System.ComponentModel.DataAnnotations;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Application.Orders;

public class OrderQuery
{
    public string? Search { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public record OrderListItem(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    string CustomerName,
    int ItemCount,
    decimal TotalAmount,
    OrderStatus Status);

public record OrderLineDetail(int ProductId, string ProductName, string Sku, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public record OrderDetail(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    IReadOnlyCollection<OrderStatus> NextStatuses,
    int CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? Notes,
    string? CreatedBy,
    decimal TotalAmount,
    IReadOnlyList<OrderLineDetail> Lines);

public class CreateOrderLine
{
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ürün seçin.")]
    public int ProductId { get; set; }

    [Range(1, 10_000, ErrorMessage = "Miktar 1 ile 10.000 arasında olmalıdır.")]
    public int Quantity { get; set; } = 1;
}

public class CreateOrderModel
{
    [Display(Name = "Müşteri")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir müşteri seçin.")]
    public int CustomerId { get; set; }

    [Display(Name = "Not")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "Siparişe en az bir ürün ekleyin.")]
    public List<CreateOrderLine> Lines { get; set; } = [];
}
