using System.Text.Json;
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
    private const string DebugLogPath = "c:\\Programing\\Projects\\Marketplace\\debug-fe2101.log";
    private const string DebugSessionId = "fe2101";
    private const string RunId = "oauth-check-1";

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
        // #region agent log
        DebugLog("H1", "ExternalAuthController.Google:entry", "Google OAuth start called", new { hasClientId = !string.IsNullOrWhiteSpace(_configuration["GoogleAuth:ClientId"]), hasClientSecret = !string.IsNullOrWhiteSpace(_configuration["GoogleAuth:ClientSecret"]), returnPath });
        // #endregion

        if (string.IsNullOrWhiteSpace(_configuration["GoogleAuth:ClientId"]))
        {
            // #region agent log
            DebugLog("H1", "ExternalAuthController.Google:missingClientId", "Google OAuth config missing", new { key = "GoogleAuth:ClientId" });
            // #endregion
            return Problem(detail: "Google OAuth is not configured (GoogleAuth section).", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(returnPath) || !returnPath.StartsWith('/'))
            returnPath = "/auth/callback";

        var appState = await _googleOAuthService.CreateAuthStateAsync(returnPath, ct);

        // #region agent log
        DebugLog("H2", "ExternalAuthController.Google:stateCreated", "OAuth app state created", new { returnPath, stateLength = appState.Length });
        // #endregion

        var props = new AuthenticationProperties
        {
            RedirectUri = Url.ActionLink(nameof(GoogleReturn), values: new { appState })
        };

        if (props.RedirectUri is null)
        {
            // #region agent log
            DebugLog("H2", "ExternalAuthController.Google:redirectUriNull", "Failed to build callback URL", new { appStateLength = appState.Length });
            // #endregion
            return Problem(detail: "Cannot build callback URL", statusCode: StatusCodes.Status500InternalServerError);
        }

        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/return")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleReturn([FromQuery] string appState, CancellationToken ct)
    {
        // #region agent log
        DebugLog("H2", "ExternalAuthController.GoogleReturn:entry", "Google return called", new { hasAppState = !string.IsNullOrWhiteSpace(appState), appStateLength = appState?.Length ?? 0 });
        // #endregion

        if (string.IsNullOrWhiteSpace(appState))
            return Problem(detail: "Missing appState", statusCode: StatusCodes.Status400BadRequest);

        var returnPath = await _googleOAuthService.ConsumeAuthStateAsync(appState, ct);
        if (returnPath is null)
        {
            // #region agent log
            DebugLog("H2", "ExternalAuthController.GoogleReturn:stateInvalid", "OAuth app state invalid/expired", new { appStateLength = appState.Length });
            // #endregion
            return Problem(detail: "Invalid or expired OAuth state", statusCode: StatusCodes.Status400BadRequest);
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            // #region agent log
            DebugLog("H3", "ExternalAuthController.GoogleReturn:externalAuthFailed", "External auth principal missing", new { succeeded = authenticateResult.Succeeded, failure = authenticateResult.Failure?.Message });
            // #endregion
            return Problem(detail: "Google external authentication failed", statusCode: StatusCodes.Status401Unauthorized);
        }

        var authResult = await _googleOAuthService.SignInOrProvisionAsync(authenticateResult.Principal, ct);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (authResult is not { IsSuccess: true, Value: not null })
        {
            // #region agent log
            DebugLog("H3", "ExternalAuthController.GoogleReturn:signInProvisionFailed", "SignInOrProvision failed", new { error = authResult.Error });
            // #endregion
            return Problem(detail: authResult.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        var code = await _googleOAuthService.CreateExchangeCodeAsync(authResult.Value, ct);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            // #region agent log
            DebugLog("H4", "ExternalAuthController.GoogleReturn:frontendMissing", "Frontend base URL missing", new { key = "Frontend:BaseUrl", returnPath });
            // #endregion
            return Problem(detail: "Frontend:BaseUrl is not configured", statusCode: StatusCodes.Status500InternalServerError);
        }

        var redirect = $"{frontendBaseUrl.TrimEnd('/')}{returnPath}?code={Uri.EscapeDataString(code)}";

        // #region agent log
        DebugLog("H4", "ExternalAuthController.GoogleReturn:redirectPrepared", "Redirect prepared with exchange code", new { returnPath, codeLength = code.Length, frontendBaseUrl });
        // #endregion

        return Redirect(redirect);
    }

    [HttpPost("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallbackPost([FromBody] GoogleCallbackExchangeRequest request, CancellationToken ct)
    {
        // #region agent log
        DebugLog("H5", "ExternalAuthController.GoogleCallbackPost:entry", "Google callback exchange called", new { hasCode = !string.IsNullOrWhiteSpace(request.Code), codeLength = request.Code?.Length ?? 0 });
        // #endregion

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new ProblemDetails { Detail = "Code is required." });

        var payload = await _googleOAuthService.ConsumeExchangeCodeAsync(request.Code, ct);
        if (payload is null)
        {
            // #region agent log
            DebugLog("H5", "ExternalAuthController.GoogleCallbackPost:codeInvalid", "Exchange code invalid/expired", new { codeLength = request.Code.Length });
            // #endregion
            return Unauthorized(new ProblemDetails { Detail = "Invalid or expired exchange code." });
        }

        Response.Cookies.Append(_cookieOptions.RefreshTokenCookieName, payload.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(_cookieOptions.RefreshTokenDays),
            Path = "/"
        });

        // #region agent log
        DebugLog("H5", "ExternalAuthController.GoogleCallbackPost:success", "Exchange code consumed and refresh cookie set", new { accessTokenLength = payload.AccessToken.Length, refreshCookieName = _cookieOptions.RefreshTokenCookieName });
        // #endregion

        return Ok(new
        {
            accessToken = payload.AccessToken,
            accessTokenExpiresAt = payload.AccessTokenExpiresAt
        });
    }

    private static void DebugLog(string hypothesisId, string location, string message, object data)
    {
        try
        {
            var payload = new
            {
                sessionId = DebugSessionId,
                runId = RunId,
                hypothesisId,
                location,
                message,
                data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var line = JsonSerializer.Serialize(payload);
            System.IO.File.AppendAllText(DebugLogPath, line + Environment.NewLine);
        }
        catch
        {
            // no-op in debug instrumentation
        }
    }
}

public record GoogleCallbackExchangeRequest(string Code);
