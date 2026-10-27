namespace InventoryHub.Domain.Common;

/// <summary>
/// Bu arayüzü uygulayan varlıklar fiziksel olarak silinmez; IsDeleted bayrağı işaretlenir
/// ve global query filter ile sorgulardan otomatik olarak dışlanır.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
