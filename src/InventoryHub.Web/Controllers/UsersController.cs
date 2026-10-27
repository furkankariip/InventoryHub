using InventoryHub.Application.Common;
using InventoryHub.Infrastructure.Identity;
using InventoryHub.Web.Infrastructure;
using InventoryHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Web.Controllers;

[Authorize(Policy = Policies.AdminOnly)]
public class UsersController(UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var items = new List<UserListItem>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new UserListItem(user.Id, user.FullName, user.Email!, roles.FirstOrDefault() ?? "-",
                user.IsActive, user.CreatedAt));
        }
        return View(items);
    }

    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!Roles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Geçersiz rol.");
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email, Email = model.Email, FullName = model.FullName, EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
            result = await userManager.AddToRoleAsync(user, model.Role);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData.SetSuccess($"{model.FullName} kullanıcısı oluşturuldu.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (user.UserName == User.Identity?.Name)
        {
            TempData.SetError("Kendi hesabınızı devre dışı bırakamazsınız.");
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await userManager.UpdateAsync(user);
        // Güvenlik damgası yenilenerek kullanıcının açık oturumları geçersiz kılınır.
        await userManager.UpdateSecurityStampAsync(user);

        TempData.SetSuccess($"{user.FullName} {(user.IsActive ? "etkinleştirildi" : "devre dışı bırakıldı")}.");
        return RedirectToAction(nameof(Index));
    }
}
