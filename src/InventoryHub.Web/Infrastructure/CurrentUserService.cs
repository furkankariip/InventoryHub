using InventoryHub.Application.Common;

namespace InventoryHub.Web.Infrastructure;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserName => httpContextAccessor.HttpContext?.User.Identity?.Name;
}
