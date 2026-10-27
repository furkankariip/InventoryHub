using System.Diagnostics;
using InventoryHub.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Web.Controllers;

public class HomeController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await dashboardService.GetAsync(ct));

    [AllowAnonymous]
    [Route("Home/StatusCode/{code:int}")]
    public new IActionResult StatusCode(int code) =>
        View(code == 404 ? "NotFound" : "Error", Activity.Current?.Id ?? HttpContext.TraceIdentifier);

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("Error", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
}
