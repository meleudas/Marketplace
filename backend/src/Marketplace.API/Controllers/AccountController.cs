using Marketplace.API.Extensions;
using Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.DisableTelegramTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.GenerateTelegramLinkCode;
using Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;
using Marketplace.Application.Auth.Commands.TwoFactor.SendTelegramTwoFactorCode;
using Marketplace.Application.Auth.Queries.GetTwoFactorStatus;
using Marketplace.Application.Users.Commands.RequestPasswordReset;
using Marketplace.Application.Users.Commands.ResetPassword;
using Marketplace.Application.Users.Commands.VerifyEmail;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ISender sender, ILogger<AccountController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "confirm-email"));
        var result = await _sender.Send(new VerifyEmailCommand(request.Email, request.Token), ct);
        RecordAuthResult("confirm-email", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "forgot-password"));
        var result = await _sender.Send(new RequestPasswordResetCommand(request.Email), ct);
        RecordAuthResult("forgot-password", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "reset-password"));
        var result = await _sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            ct);
        RecordAuthResult("reset-password", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorStatus(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-status"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-status", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new GetTwoFactorStatusQuery(identityUserId), ct);
        RecordAuthResult("2fa-status", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/send-code")]
    [Authorize]
    public async Task<IActionResult> SendTwoFactorCode(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-email-send-code"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-email-send-code", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new SendEmailTwoFactorCodeCommand(identityUserId), ct);
        RecordAuthResult("2fa-email-send-code", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableEmailTwoFactorRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-email-enable"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-email-enable", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new EnableEmailTwoFactorCommand(identityUserId, request.Code), ct);
        RecordAuthResult("2fa-email-enable", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-email-disable"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-email-disable", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new DisableEmailTwoFactorCommand(identityUserId), ct);
        RecordAuthResult("2fa-email-disable", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/link-code")]
    [Authorize]
    public async Task<IActionResult> GenerateTelegramLinkCode(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-telegram-link-code"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-telegram-link-code", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new GenerateTelegramLinkCodeCommand(identityUserId), ct);
        RecordAuthResult("2fa-telegram-link-code", result.IsSuccess, result.Error);
        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        return Ok(new TelegramLinkCodeResponse(result.Value));
    }

    [HttpPost("2fa/telegram/send-code")]
    [Authorize]
    public async Task<IActionResult> SendTelegramTwoFactorCode(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-telegram-send-code"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-telegram-send-code", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new SendTelegramTwoFactorCodeCommand(identityUserId), ct);
        RecordAuthResult("2fa-telegram-send-code", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTelegramTwoFactor([FromBody] EnableTelegramTwoFactorRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-telegram-enable"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-telegram-enable", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new EnableTelegramTwoFactorCommand(identityUserId, request.Code), ct);
        RecordAuthResult("2fa-telegram-enable", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTelegramTwoFactor(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(
            MarketplaceMetrics.AuthLatencyMs,
            new KeyValuePair<string, object?>("operation", "2fa-telegram-disable"));
        if (!User.TryGetUserId(out var identityUserId))
        {
            RecordAuthResult("2fa-telegram-disable", false, "unauthorized");
            return Unauthorized();
        }

        var result = await _sender.Send(new DisableTelegramTwoFactorCommand(identityUserId), ct);
        RecordAuthResult("2fa-telegram-disable", result.IsSuccess, result.Error);
        return result.ToActionResult();
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
        _logger.LogWarning("Account operation {Operation} failed: {Error}", operation, error ?? "unknown_error");
    }
}

public record ConfirmEmailRequest(string Email, string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record EnableEmailTwoFactorRequest(string Code);
public record EnableTelegramTwoFactorRequest(string Code);
public record TelegramLinkCodeResponse(string LinkCode);
