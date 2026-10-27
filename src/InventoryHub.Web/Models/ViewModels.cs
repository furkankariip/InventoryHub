using System.ComponentModel.DataAnnotations;
using InventoryHub.Application.Common;

namespace InventoryHub.Web.Models;

/// <summary>Filtre + sayfalı sonuç gösteren liste sayfaları için ortak model.</summary>
public record ListViewModel<TQuery, TItem>(TQuery Query, PagedResult<TItem> Result);

public class LoginViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public record UserListItem(string Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt);

public class CreateUserViewModel
{
    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} en az {2} karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    public string Role { get; set; } = Roles.Staff;
}
