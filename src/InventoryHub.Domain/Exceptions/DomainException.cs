namespace InventoryHub.Domain.Exceptions;

/// <summary>İş kuralı ihlallerinde fırlatılır; kullanıcıya gösterilebilecek bir mesaj taşır.</summary>
public class DomainException(string message) : Exception(message);
