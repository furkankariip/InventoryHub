namespace InventoryHub.Application.Common;

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} ({key}) bulunamadı.");
