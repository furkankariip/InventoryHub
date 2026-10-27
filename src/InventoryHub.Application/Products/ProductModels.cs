using System.ComponentModel.DataAnnotations;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Application.Products;

public enum ProductSort
{
    Name,
    NameDesc,
    Price,
    PriceDesc,
    Stock,
    StockDesc,
    Newest
}

public class ProductQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool LowStockOnly { get; set; }
    public ProductSort Sort { get; set; } = ProductSort.Name;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public record ProductListItem(
    int Id,
    string Name,
    string Sku,
    string CategoryName,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderLevel,
    bool IsActive)
{
    public bool IsLowStock => StockQuantity <= ReorderLevel;
}

public record ProductStockMovementItem(
    DateTime CreatedAt,
    StockMovementType Type,
    int Quantity,
    int StockAfter,
    string? Description,
    string? CreatedBy);

public record ProductDetail(
    int Id,
    string Name,
    string Sku,
    string? Description,
    string CategoryName,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderLevel,
    bool IsActive,
    DateTime CreatedAt,
    int TotalSold,
    IReadOnlyList<ProductStockMovementItem> RecentMovements)
{
    public bool IsLowStock => StockQuantity <= ReorderLevel;
}

public record OrderableProduct(int Id, string Name, string Sku, decimal UnitPrice, int StockQuantity);

public class ProductFormModel
{
    public int Id { get; set; }

    [Display(Name = "Ürün Adı")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Stok Kodu (SKU)")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(30, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "{0} yalnızca harf, rakam ve '-' içerebilir.")]
    public string Sku { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(1000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }

    [Display(Name = "Birim Fiyat (₺)")]
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true, ErrorMessage = "{0} {1} ile {2} arasında olmalıdır.")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Kritik Stok Seviyesi")]
    [Range(0, 100_000, ErrorMessage = "{0} {1} ile {2} arasında olmalıdır.")]
    public int ReorderLevel { get; set; } = 10;

    [Display(Name = "Başlangıç Stoğu")]
    [Range(0, 1_000_000, ErrorMessage = "{0} {1} ile {2} arasında olmalıdır.")]
    public int InitialStock { get; set; }

    [Display(Name = "Kategori")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Satışta")]
    public bool IsActive { get; set; } = true;

    public Guid ConcurrencyStamp { get; set; }
}
