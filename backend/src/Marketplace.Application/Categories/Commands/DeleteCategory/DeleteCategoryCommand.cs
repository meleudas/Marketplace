using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(long CategoryId) : IRequest<Result>;
