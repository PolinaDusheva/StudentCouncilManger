using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Api.Filters;

/// <summary>
/// Enforces spec 6.4: while a member still has <c>MustChangePassword = true</c>, every endpoint
/// except those marked <see cref="BypassPasswordChangeGateAttribute"/> (and anonymous ones)
/// returns <c>403 password_change_required</c>.
/// </summary>
public sealed class RequirePasswordChangeFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true
            && user.HasClaim("must_change_password", "true")
            && !IsExempt(context))
        {
            throw new PasswordChangeRequiredException();
        }

        await next();
    }

    private static bool IsExempt(ActionExecutingContext context)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        return metadata.OfType<BypassPasswordChangeGateAttribute>().Any()
            || metadata.OfType<IAllowAnonymous>().Any();
    }
}
