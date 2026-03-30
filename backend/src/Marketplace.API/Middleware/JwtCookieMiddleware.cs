using Marketplace.API.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Middleware;

/// <summary>
/// Прокидає refresh-токен з HTTP-only cookie в <see cref="HttpContext.Items"/> для ендпоінтів,
/// де тіло запиту може не містити токен (наприклад SPA).
/// </summary>
public sealed class JwtCookieMiddleware
{
    public const string RefreshTokenItemKey = "RefreshTokenFromCookie";

    private readonly RequestDelegate _next;
    private readonly CookieAuthOptions _options;

    public JwtCookieMiddleware(RequestDelegate next, IOptions<CookieAuthOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(_options.RefreshTokenCookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
            context.Items[RefreshTokenItemKey] = token;

        return _next(context);
    }
}
