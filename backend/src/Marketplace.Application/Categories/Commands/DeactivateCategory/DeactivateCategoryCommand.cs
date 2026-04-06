using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.DeactivateCategory;

public sealed record DeactivateCategoryCommand(long CategoryId) : IRequest<Result>;
