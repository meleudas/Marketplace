using FluentValidation;

namespace Marketplace.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Category slug is required");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Category sort order cannot be negative");
    }
}
