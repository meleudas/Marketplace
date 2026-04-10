using Marketplace.API.Options;
using Marketplace.Infrastructure.External.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("auth")]
public class ExternalAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly GoogleOAuthService _googleOAuthService;
    private readonly CookieAuthOptions _cookieOptions;

    public ExternalAuthController(
        IConfiguration configuration,
        GoogleOAuthService googleOAuthService,
        IOptions<CookieAuthOptions> cookieOptions)
    {
        _configuration = configuration;
        _googleOAuthService = googleOAuthService;
        _cookieOptions = cookieOptions.Value;
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google([FromQuery] string? returnPath = "/auth/callback", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_configuration["GoogleAuth:ClientId"]))
            return Problem(detail: "Google OAuth is not configured (GoogleAuth section).", statusCode: StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrWhiteSpace(returnPath) || !returnPath.StartsWith('/'))
            returnPath = "/auth/callback";

        var appState = await _googleOAuthService.CreateAuthStateAsync(returnPath, ct);

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.ActionLink(nameof(GoogleReturn), values: new { appState })
        };

        if (props.RedirectUri is null)
            return Problem(detail: "Cannot build callback URL", statusCode: StatusCodes.Status500InternalServerError);

        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/return")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleReturn([FromQuery] string appState, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appState))
            return Problem(detail: "Missing appState", statusCode: StatusCodes.Status400BadRequest);

        var returnPath = await _googleOAuthService.ConsumeAuthStateAsync(appState, ct);
        if (returnPath is null)
            return Problem(detail: "Invalid or expired OAuth state", statusCode: StatusCodes.Status400BadRequest);

        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            return Problem(detail: "Google external authentication failed", statusCode: StatusCodes.Status401Unauthorized);

        var authResult = await _googleOAuthService.SignInOrProvisionAsync(authenticateResult.Principal, ct);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (authResult is not { IsSuccess: true, Value: not null })
            return Problem(detail: authResult.Error, statusCode: StatusCodes.Status401Unauthorized);

        var code = await _googleOAuthService.CreateExchangeCodeAsync(authResult.Value, ct);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            return Problem(detail: "Frontend:BaseUrl is not configured", statusCode: StatusCodes.Status500InternalServerError);

        var redirect = $"{frontendBaseUrl.TrimEnd('/')}{returnPath}?code={Uri.EscapeDataString(code)}";

        return Redirect(redirect);
    }

    [HttpPost("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallbackPost([FromBody] GoogleCallbackExchangeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new ProblemDetails { Detail = "Code is required." });

        var payload = await _googleOAuthService.ConsumeExchangeCodeAsync(request.Code, ct);
        if (payload is null)
            return Unauthorized(new ProblemDetails { Detail = "Invalid or expired exchange code." });

        Response.Cookies.Append(_cookieOptions.RefreshTokenCookieName, payload.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(_cookieOptions.RefreshTokenDays),
            Path = "/"
        });

        return Ok(new
        {
            accessToken = payload.AccessToken,
            accessTokenExpiresAt = payload.AccessTokenExpiresAt
        });
    }
}

public record GoogleCallbackExchangeRequest(string Code);
