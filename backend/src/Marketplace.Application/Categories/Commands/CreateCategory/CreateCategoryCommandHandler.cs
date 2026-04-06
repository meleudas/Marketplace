using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Categories.Mappings;
using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var id = CategoryId.From(0);

            var category = Category.Create(
                id,
                request.Name,
                request.Slug,
                request.ImageUrl,
                request.ParentCategoryId.HasValue ? CategoryId.From(request.ParentCategoryId.Value) : null,
                request.Description,
                new JsonBlob(request.MetaRaw),
                request.SortOrder,
                request.IsActive);

            var createdCategory = await _categoryRepository.AddAsync(category, ct);
            return Result<CategoryDto>.Success(CategoryMapper.ToDto(createdCategory));
        }
        catch (Exception ex)
        {
            return Result<CategoryDto>.Failure($"Failed to create category: {ex.Message}");
        }
    }
}
