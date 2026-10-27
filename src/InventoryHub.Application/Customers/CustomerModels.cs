using System.ComponentModel.DataAnnotations;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Application.Customers;

public class CustomerQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public record CustomerListItem(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string? City,
    int OrderCount,
    decimal TotalSpent);

public record CustomerOrderItem(int Id, string OrderNumber, DateTime OrderDate, OrderStatus Status, decimal TotalAmount);

public record CustomerDetail(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string? City,
    string? Address,
    DateTime CreatedAt,
    decimal TotalSpent,
    IReadOnlyList<CustomerOrderItem> Orders);

public class CustomerFormModel
{
    public int Id { get; set; }

    [Display(Name = "Ad Soyad / Firma")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Şehir")]
    [StringLength(50)]
    public string? City { get; set; }

    [Display(Name = "Adres")]
    [StringLength(500)]
    public string? Address { get; set; }
}
