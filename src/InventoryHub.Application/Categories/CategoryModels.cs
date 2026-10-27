using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Application.Categories;

public record CategoryListItem(int Id, string Name, string? Description, int ProductCount);

public class CategoryFormModel
{
    public int Id { get; set; }

    [Display(Name = "Kategori Adı")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(100, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }
}
