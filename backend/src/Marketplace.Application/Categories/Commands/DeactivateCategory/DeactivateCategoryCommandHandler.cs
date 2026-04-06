using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;

    public DeactivateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
            if (category == null)
                return Result.Failure("Category not found");

            category.Deactivate();
            await _categoryRepository.UpdateAsync(category, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to deactivate category: {ex.Message}");
        }
    }
}
