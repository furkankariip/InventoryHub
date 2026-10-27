using InventoryHub.Domain.Common;
using InventoryHub.Domain.Exceptions;

namespace InventoryHub.Domain.Entities;

public class Product : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; private set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>Optimistic concurrency için kullanılır; her güncellemede yenilenir.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public bool IsLowStock => StockQuantity <= ReorderLevel;

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Miktar sıfırdan büyük olmalıdır.");
        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Miktar sıfırdan büyük olmalıdır.");
        if (quantity > StockQuantity)
            throw new DomainException($"'{Name}' için yetersiz stok. Mevcut: {StockQuantity}, istenen: {quantity}.");
        StockQuantity -= quantity;
    }

    /// <summary>Sayım sonrası stoğu doğrudan belirler ve aradaki farkı döner.</summary>
    public int SetStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new DomainException("Stok miktarı negatif olamaz.");
        var difference = newQuantity - StockQuantity;
        StockQuantity = newQuantity;
        return difference;
    }
}
