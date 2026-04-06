using FluentValidation;

namespace Marketplace.Application.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
{
    public DeactivateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category id must be greater than 0");
    }
}
