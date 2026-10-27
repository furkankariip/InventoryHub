using InventoryHub.Application.Products;
using InventoryHub.Application.Stock;
using InventoryHub.Domain.Exceptions;
using InventoryHub.Web.Infrastructure;
using InventoryHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryHub.Web.Controllers;

public class StockController(IStockService stockService, IProductService productService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] StockMovementQuery query, CancellationToken ct)
    {
        var result = await stockService.GetMovementsAsync(query, ct);
        await LoadProductsAsync(query.ProductId, ct);
        return View(new ListViewModel<StockMovementQuery, StockMovementListItem>(query, result));
    }

    public async Task<IActionResult> Adjust(int? productId, CancellationToken ct)
    {
        await LoadProductsAsync(productId, ct);
        return View(new StockAdjustmentModel { ProductId = productId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Adjust(StockAdjustmentModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await stockService.AdjustAsync(model, ct);
                TempData.SetSuccess("Stok hareketi kaydedildi.");
                return RedirectToAction("Details", "Products", new { id = model.ProductId });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await LoadProductsAsync(model.ProductId, ct);
        return View(model);
    }

    private async Task LoadProductsAsync(int? selectedId, CancellationToken ct)
    {
        var products = await productService.GetLookupAsync(ct);
        ViewBag.Products = new SelectList(products, "Id", "Name", selectedId);
    }
}
