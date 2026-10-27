using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Domain.Enums;

public enum OrderStatus
{
    [Display(Name = "Beklemede")] Pending = 0,
    [Display(Name = "Onaylandı")] Confirmed = 1,
    [Display(Name = "Kargoda")] Shipped = 2,
    [Display(Name = "Teslim Edildi")] Delivered = 3,
    [Display(Name = "İptal Edildi")] Cancelled = 4
}
