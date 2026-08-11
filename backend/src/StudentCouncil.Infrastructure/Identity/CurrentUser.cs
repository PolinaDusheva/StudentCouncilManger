using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>Resolves the caller from the validated JWT claims on the current request.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? Id
    {
        get
        {
            var value = Principal?.FindFirstValue("sub")
                ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue("email")
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public SystemRole? Role
    {
        get
        {
            var value = Principal?.FindFirstValue("role");
            return Enum.TryParse<SystemRole>(value, out var role) ? role : null;
        }
    }

    public Guid? DepartmentId
    {
        get
        {
            var value = Principal?.FindFirstValue("deptId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
