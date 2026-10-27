using InventoryHub.Application.Customers;
using InventoryHub.Domain.Exceptions;
using InventoryHub.Web.Infrastructure;
using InventoryHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Web.Controllers;

public class CustomersController(ICustomerService customerService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] CustomerQuery query, CancellationToken ct)
    {
        var result = await customerService.GetPagedAsync(query, ct);
        return View(new ListViewModel<CustomerQuery, CustomerListItem>(query, result));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct) =>
        View(await customerService.GetDetailAsync(id, ct));

    public IActionResult Create() => View("Form", new CustomerFormModel());

    [HttpPost]
    public async Task<IActionResult> Create(CustomerFormModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            var id = await customerService.CreateAsync(model, ct);
            TempData.SetSuccess($"'{model.FullName}' müşterisi eklendi.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(nameof(model.Email), ex.Message);
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct) =>
        View("Form", await customerService.GetForEditAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CustomerFormModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            await customerService.UpdateAsync(model, ct);
            TempData.SetSuccess("Müşteri bilgileri güncellendi.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(nameof(model.Email), ex.Message);
            return View("Form", model);
        }
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await customerService.DeleteAsync(id, ct);
        TempData.SetSuccess("Müşteri silindi.");
        return RedirectToAction(nameof(Index));
    }
}
