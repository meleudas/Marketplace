using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.GetShippingMethods;

public sealed record GetShippingMethodsQuery() : IRequest<Result<IReadOnlyList<ShippingMethodDto>>>;
