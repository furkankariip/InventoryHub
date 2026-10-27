using InventoryHub.Application.Categories;
using InventoryHub.Domain.Exceptions;
using InventoryHub.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Web.Controllers;

public class CategoriesController(ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await categoryService.GetAllAsync(ct));

    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult Create() => View("Form", new CategoryFormModel());

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            await categoryService.CreateAsync(model, ct);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View("Form", model);
        }

        TempData.SetSuccess($"'{model.Name}' kategorisi oluşturuldu.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct) =>
        View("Form", await categoryService.GetForEditAsync(id, ct));

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            await categoryService.UpdateAsync(model, ct);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View("Form", model);
        }

        TempData.SetSuccess("Kategori güncellendi.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(id, ct);
        TempData.SetSuccess("Kategori silindi.");
        return RedirectToAction(nameof(Index));
    }
}
