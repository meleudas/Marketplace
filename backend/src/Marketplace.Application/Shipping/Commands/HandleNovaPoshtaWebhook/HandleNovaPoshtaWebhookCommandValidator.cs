using FluentValidation;

namespace Marketplace.Application.Shipping.Commands.HandleNovaPoshtaWebhook;

public sealed class HandleNovaPoshtaWebhookCommandValidator : AbstractValidator<HandleNovaPoshtaWebhookCommand>
{
    public HandleNovaPoshtaWebhookCommandValidator()
    {
        RuleFor(x => x.EventKey).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PayloadHash).NotEmpty().MaximumLength(128);
        RuleFor(x => x.RawPayload).NotEmpty();
    }
}
