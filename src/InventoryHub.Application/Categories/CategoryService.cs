using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<CategoryFormModel> GetForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CategoryFormModel model, CancellationToken ct = default);
    Task UpdateAsync(CategoryFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class CategoryService(IApplicationDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListItem>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItem(c.Id, c.Name, c.Description, c.Products.Count))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<CategoryFormModel> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException(nameof(Category), id);

        return new CategoryFormModel { Id = category.Id, Name = category.Name, Description = category.Description };
    }

    public async Task<int> CreateAsync(CategoryFormModel model, CancellationToken ct = default)
    {
        await EnsureUniqueNameAsync(model.Name, null, ct);

        var category = new Category { Name = model.Name.Trim(), Description = model.Description?.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task UpdateAsync(CategoryFormModel model, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([model.Id], ct)
                       ?? throw new NotFoundException(nameof(Category), model.Id);

        await EnsureUniqueNameAsync(model.Name, model.Id, ct);

        category.Name = model.Name.Trim();
        category.Description = model.Description?.Trim();
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct)
                       ?? throw new NotFoundException(nameof(Category), id);

        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new DomainException("Bu kategoriye bağlı ürünler var. Önce ürünleri taşıyın veya silin.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var normalized = name.Trim();
        var exists = await db.Categories.AnyAsync(c => c.Name == normalized && c.Id != excludeId, ct);
        if (exists)
            throw new DomainException($"'{normalized}' adında bir kategori zaten mevcut.");
    }
}
