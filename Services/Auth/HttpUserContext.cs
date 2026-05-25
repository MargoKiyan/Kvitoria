using System.Security.Claims;
using Kvitoria.Models.Auth;

namespace Kvitoria.Services.Auth;

public class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(ApplicationRoleNames.Admin) == true;

    public bool IsUser => httpContextAccessor.HttpContext?.User.IsInRole(ApplicationRoleNames.User) == true;
}
