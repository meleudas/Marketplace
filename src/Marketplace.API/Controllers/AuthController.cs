using System.Security.Claims;
using Marketplace.API.Extensions;
using Marketplace.API.Middleware;
using Marketplace.API.Options;
using Marketplace.Application.Auth.Commands.Login;
using Marketplace.Application.Auth.Commands.Logout;
using Marketplace.Application.Auth.Commands.RefreshToken;
using Marketplace.Application.Auth.Commands.Register;
using Marketplace.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly CookieAuthOptions _cookieOptions;

    public AuthController(ISender sender, IOptions<CookieAuthOptions> cookieOptions)
    {
        _sender = sender;
        _cookieOptions = cookieOptions.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new RegisterCommand(request.Email, request.Password, request.UserName, request.PhoneNumber),
            ct);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password), ct);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        var fromCookie = HttpContext.Items[JwtCookieMiddleware.RefreshTokenItemKey] as string;
        var refresh = request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refresh))
            refresh = fromCookie;

        var result = await _sender.Send(new RefreshTokenCommand(refresh), ct);

        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        AppendRefreshCookie(result.Value.RefreshToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var result = await _sender.Send(new LogoutCommand(userId), ct);
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
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string? RefreshToken);
