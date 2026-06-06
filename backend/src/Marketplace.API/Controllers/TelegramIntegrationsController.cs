using Marketplace.Application.Auth.Commands.TwoFactor.LinkTelegramAccount;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Auth.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("integrations/telegram")]
[AllowAnonymous]
public sealed class TelegramIntegrationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TelegramOptions _options;
    private readonly IWebHostEnvironment _environment;

    public TelegramIntegrationsController(ISender sender, IOptions<TelegramOptions> options, IWebHostEnvironment environment)
    {
        _sender = sender;
        _options = options.Value;
        _environment = environment;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdateDto update, CancellationToken ct)
    {
        if (!_environment.IsDevelopment() && string.IsNullOrWhiteSpace(_options.WebhookSecret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            var header = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
            if (!string.Equals(header, _options.WebhookSecret, StringComparison.Ordinal))
                return Unauthorized();
        }

        var chatId = update.Message?.Chat?.Id;
        var text = update.Message?.Text?.Trim();
        if (chatId is null || string.IsNullOrWhiteSpace(text))
            return Ok();

        if (!text.StartsWith("/start ", StringComparison.OrdinalIgnoreCase))
            return Ok();

        var linkCode = text[7..].Trim();
        if (string.IsNullOrWhiteSpace(linkCode))
            return Ok();

        var result = await _sender.Send(new LinkTelegramAccountCommand(linkCode, chatId.Value.ToString()), ct);
        return result.IsSuccess ? Ok() : BadRequest();
    }
}
