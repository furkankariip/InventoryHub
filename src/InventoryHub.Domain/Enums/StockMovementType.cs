using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Domain.Enums;

public enum StockMovementType
{
    [Display(Name = "Stok Girişi")] Purchase = 0,
    [Display(Name = "Satış")] Sale = 1,
    [Display(Name = "İade")] Return = 2,
    [Display(Name = "Sayım Düzeltmesi")] Adjustment = 3
}
