using FluentValidation;

namespace Marketplace.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category id must be greater than 0");

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
