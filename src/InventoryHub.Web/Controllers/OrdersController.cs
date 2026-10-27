using InventoryHub.Application.Customers;
using InventoryHub.Application.Orders;
using InventoryHub.Application.Products;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using InventoryHub.Web.Infrastructure;
using InventoryHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryHub.Web.Controllers;

public class OrdersController(
    IOrderService orderService,
    ICustomerService customerService,
    IProductService productService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] OrderQuery query, CancellationToken ct)
    {
        var result = await orderService.GetPagedAsync(query, ct);
        return View(new ListViewModel<OrderQuery, OrderListItem>(query, result));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct) =>
        View(await orderService.GetDetailAsync(id, ct));

    public async Task<IActionResult> Create(int? customerId, CancellationToken ct)
    {
        var model = new CreateOrderModel { CustomerId = customerId ?? 0, Lines = [new CreateOrderLine()] };
        await LoadFormDataAsync(model.CustomerId, ct);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await orderService.CreateAsync(model, ct);
                TempData.SetSuccess("Sipariş oluşturuldu ve stoklar güncellendi.");
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DomainException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        if (model.Lines.Count == 0)
            model.Lines.Add(new CreateOrderLine());

        await LoadFormDataAsync(model.CustomerId, ct);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status, CancellationToken ct)
    {
        await orderService.ChangeStatusAsync(id, status, ct);
        TempData.SetSuccess(status == OrderStatus.Cancelled
            ? "Sipariş iptal edildi; ürünler stoğa iade edildi."
            : $"Sipariş durumu '{status.DisplayName()}' olarak güncellendi.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadFormDataAsync(int selectedCustomerId, CancellationToken ct)
    {
        var customers = await customerService.GetLookupAsync(ct);
        ViewBag.Customers = new SelectList(customers, "Id", "Name", selectedCustomerId);
        ViewBag.Products = await productService.GetOrderableAsync(ct);
    }
}
