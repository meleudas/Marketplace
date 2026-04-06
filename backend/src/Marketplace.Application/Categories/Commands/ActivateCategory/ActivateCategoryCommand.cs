using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.ActivateCategory;

public sealed record ActivateCategoryCommand(long CategoryId) : IRequest<Result>;
