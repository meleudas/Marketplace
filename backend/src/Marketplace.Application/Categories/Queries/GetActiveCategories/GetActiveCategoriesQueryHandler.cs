using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetActiveCategories;

public sealed class GetActiveCategoriesQueryHandler : IRequestHandler<GetActiveCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetActiveCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetActiveCategoriesQuery request, CancellationToken ct)
    {
        try
        {
            var categories = await _categoryRepository.GetActiveAsync(ct);
            var dtos = categories.Select(CategoryMapper.ToDto).ToList();
            return Result<IReadOnlyList<CategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<CategoryDto>>.Failure($"Failed to get active categories: {ex.Message}");
        }
    }
}
