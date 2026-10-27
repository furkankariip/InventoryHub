using System.ComponentModel.DataAnnotations;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Application.Stock;

public class StockAdjustmentModel
{
    [Display(Name = "Ürün")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ürün seçin.")]
    public int ProductId { get; set; }

    [Display(Name = "İşlem Türü")]
    public StockMovementType Type { get; set; } = StockMovementType.Purchase;

    /// <summary>Giriş/iade için eklenecek miktar; sayım düzeltmesi için yeni stok miktarı.</summary>
    [Display(Name = "Miktar")]
    [Range(0, 1_000_000, ErrorMessage = "{0} {1} ile {2} arasında olmalıdır.")]
    public int Quantity { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(250, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }
}

public class StockMovementQuery
{
    public int? ProductId { get; set; }
    public StockMovementType? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public record StockMovementListItem(
    int Id,
    DateTime CreatedAt,
    int ProductId,
    string ProductName,
    string Sku,
    StockMovementType Type,
    int Quantity,
    int StockAfter,
    string? Description,
    string? CreatedBy,
    int? OrderId);
