using InventoryHub.Domain.Common;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Domain.Entities;

/// <summary>Ürün stoğundaki her değişikliğin değiştirilemez kaydı (audit trail).</summary>
public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>Pozitif değer stok girişi, negatif değer stok çıkışıdır.</summary>
    public int Quantity { get; set; }

    public int StockAfter { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}
