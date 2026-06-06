using Marketplace.API.Extensions;
using Marketplace.API.Middleware;
using Marketplace.API.Options;
using Marketplace.Application.Auth.Commands.Login;
using Marketplace.Application.Auth.Commands.Logout;
using Marketplace.Application.Auth.Commands.RefreshToken;
using Marketplace.Application.Auth.Commands.Register;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Auth")]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly CookieAuthOptions _cookieOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISender sender, IOptions<CookieAuthOptions> cookieOptions, ILogger<AuthController> logger)
    {
        _sender = sender;
        _cookieOptions = cookieOptions.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "register"));
        var result = await _sender.Send(
            new RegisterCommand(request.Email, request.Password, request.UserName, request.PhoneNumber),
            ct);
        RecordAuthResult("register", result.IsSuccess, result.Error);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "login"));
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password, request.RememberMe, request.TwoFactorCode), ct);
        RecordAuthResult("login", result.IsSuccess, result.Error);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "refresh"));
        var fromCookie = HttpContext.Items[JwtCookieMiddleware.RefreshTokenItemKey] as string;
        var refresh = request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refresh))
            refresh = fromCookie;

        var result = await _sender.Send(new RefreshTokenCommand(refresh), ct);
        RecordAuthResult("refresh", result.IsSuccess, result.Error);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "logout"));
        if (!User.TryGetUserId(out var userId))
        {
            RecordAuthResult("logout", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new LogoutCommand(userId), ct);
        RecordAuthResult("logout", result.IsSuccess, result.Error);
        if (!result.IsSuccess)
            return result.ToActionResult();

        Response.Cookies.Delete(_cookieOptions.RefreshTokenCookieName, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Ok();
    }

    private void RecordAuthResult(string operation, bool success, string? error)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "ok" : "error")
        };
        MarketplaceMetrics.AuthOps.Add(1, tags);
        if (success)
            return;

        MarketplaceMetrics.AuthErrors.Add(1, tags);
        _logger.LogWarning("Auth operation {Operation} failed: {Error}", operation, error ?? "unknown_error");
    }

    private void AppendRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(_cookieOptions.RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(_cookieOptions.RefreshTokenDays),
            Path = "/"
        });
    }
}

public record RegisterRequest(string Email, string Password, string UserName, string? PhoneNumber = null);
public record LoginRequest(string Email, string Password, bool RememberMe = false, string? TwoFactorCode = null);
public record RefreshRequest(string? RefreshToken);
