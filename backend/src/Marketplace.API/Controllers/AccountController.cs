using Marketplace.API.Extensions;
using Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.DisableTelegramTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.GenerateTelegramLinkCode;
using Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;
using Marketplace.Application.Auth.Commands.TwoFactor.SendTelegramTwoFactorCode;
using Marketplace.Application.Users.Commands.RequestPasswordReset;
using Marketplace.Application.Users.Commands.ResetPassword;
using Marketplace.Application.Users.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    private readonly ISender _sender;

    public AccountController(ISender sender) => _sender = sender;

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new VerifyEmailCommand(request.Email, request.Token), ct);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RequestPasswordResetCommand(request.Email), ct);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/send-code")]
    [Authorize]
    public async Task<IActionResult> SendTwoFactorCode(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new SendEmailTwoFactorCodeCommand(identityUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableEmailTwoFactorRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new EnableEmailTwoFactorCommand(identityUserId, request.Code), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new DisableEmailTwoFactorCommand(identityUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/link-code")]
    [Authorize]
    public async Task<IActionResult> GenerateTelegramLinkCode(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new GenerateTelegramLinkCodeCommand(identityUserId), ct);
        if (result is not { IsSuccess: true, Value: not null })
            return result.ToActionResult();

        return Ok(new TelegramLinkCodeResponse(result.Value));
    }

    [HttpPost("2fa/telegram/send-code")]
    [Authorize]
    public async Task<IActionResult> SendTelegramTwoFactorCode(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new SendTelegramTwoFactorCodeCommand(identityUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTelegramTwoFactor([FromBody] EnableTelegramTwoFactorRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new EnableTelegramTwoFactorCommand(identityUserId, request.Code), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/telegram/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTelegramTwoFactor(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new DisableTelegramTwoFactorCommand(identityUserId), ct);
        return result.ToActionResult();
    }
}

public record ConfirmEmailRequest(string Email, string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record EnableEmailTwoFactorRequest(string Code);
public record EnableTelegramTwoFactorRequest(string Code);
public record TelegramLinkCodeResponse(string LinkCode);
