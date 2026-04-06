using FluentValidation;

namespace Marketplace.Application.Categories.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandValidator : AbstractValidator<ActivateCategoryCommand>
{
    public ActivateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category id must be greater than 0");
    }
}
