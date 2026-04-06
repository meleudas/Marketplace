using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;

    public ActivateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result> Handle(ActivateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category == null)
                return Result.Failure("Category not found");

            category.Activate();
            await _categoryRepository.UpdateAsync(category, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to activate category: {ex.Message}");
        }
    }
}
