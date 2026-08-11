namespace StudentCouncil.Api.Middleware;

/// <summary>
/// Adds defensive HTTP response headers (spec 13). The baseline headers are cheap and harmless for the
/// JSON API (mobile clients ignore them), while <c>Content-Security-Policy</c> is only meaningful for
/// server-rendered HTML (the password-reset form) and is never applied to the Swagger UI, which relies on
/// inline scripts/styles. Headers are set in an <c>OnStarting</c> callback so the final content type is known.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            var contentType = ctx.Response.ContentType;
            if (contentType is not null
                && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase)
                && !ctx.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";
            }

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
