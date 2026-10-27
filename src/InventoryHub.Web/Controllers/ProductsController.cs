using InventoryHub.Application.Categories;
using InventoryHub.Application.Products;
using InventoryHub.Domain.Exceptions;
using InventoryHub.Web.Infrastructure;
using InventoryHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryHub.Web.Controllers;

public class ProductsController(IProductService productService, ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ProductQuery query, CancellationToken ct)
    {
        var result = await productService.GetPagedAsync(query, ct);
        await LoadCategoriesAsync(query.CategoryId, ct);
        return View(new ListViewModel<ProductQuery, ProductListItem>(query, result));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct) =>
        View(await productService.GetDetailAsync(id, ct));

    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadCategoriesAsync(null, ct);
        return View("Form", new ProductFormModel());
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(ProductFormModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await productService.CreateAsync(model, ct);
                TempData.SetSuccess($"'{model.Name}' ürünü oluşturuldu.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadCategoriesAsync(model.CategoryId, ct);
        return View("Form", model);
    }

    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await productService.GetForEditAsync(id, ct);
        await LoadCategoriesAsync(model.CategoryId, ct);
        return View("Form", model);
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductFormModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();

        // Başlangıç stoğu yalnızca yeni ürün için anlamlıdır.
        ModelState.Remove(nameof(model.InitialStock));

        if (ModelState.IsValid)
        {
            try
            {
                await productService.UpdateAsync(model, ct);
                TempData.SetSuccess("Ürün güncellendi.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadCategoriesAsync(model.CategoryId, ct);
        return View("Form", model);
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, ct);
        TempData.SetSuccess("Ürün silindi (arşivlendi). Geçmiş sipariş ve stok kayıtları korunur.");
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(int? selectedId, CancellationToken ct)
    {
        var categories = await categoryService.GetLookupAsync(ct);
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }
}
