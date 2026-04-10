using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Queries.GetProductAvailability;

public sealed record GetProductAvailabilityQuery(Guid CompanyId, long ProductId) : IRequest<Result<ProductAvailabilityDto>>;
