using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Products.Ports;

public interface IPersonalizedRecommendationService
{
    Task<Result<PersonalizedRecommendationsResultDto>> GetForUserAsync(
        Guid userId,
        int limit,
        CancellationToken ct = default);
}
