using Marketplace.API.Extensions;
using Marketplace.Application.Auth.Commands.TwoFactor.DisableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.SendEmailTwoFactorCode;
using Marketplace.Application.Users.Commands.RequestPasswordReset;
using Marketplace.Application.Users.Commands.ResetPassword;
using Marketplace.Application.Users.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new SendEmailTwoFactorCodeCommand(identityUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableEmailTwoFactorRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new EnableEmailTwoFactorCommand(identityUserId, request.Code), ct);
        return result.ToActionResult();
    }

    [HttpPost("2fa/email/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var identityUserId))
            return Unauthorized();

        var result = await _sender.Send(new DisableEmailTwoFactorCommand(identityUserId), ct);
        return result.ToActionResult();
    }
}

public record ConfirmEmailRequest(string Email, string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record EnableEmailTwoFactorRequest(string Code);
