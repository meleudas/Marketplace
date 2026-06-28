using FluentValidation;
using Marketplace.Application.Support;
using Marketplace.Application.Support.DTOs;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Queries.GetSupportTicketById;

public sealed record GetSupportTicketByIdQuery(string ActorUserId, long TicketId, bool IsStaff) : IRequest<Result<SupportTicketDetailDto>>;

public sealed class GetSupportTicketByIdQueryValidator : AbstractValidator<GetSupportTicketByIdQuery>
{
    public GetSupportTicketByIdQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TicketId).GreaterThan(0);
    }
}

public sealed class GetSupportTicketByIdQueryHandler : IRequestHandler<GetSupportTicketByIdQuery, Result<SupportTicketDetailDto>>
{
    private readonly ISupportTicketMessageRepository _messageRepository;
    private readonly SupportTicketAccessPolicy _accessPolicy;
    private readonly SupportOptions _options;

    public GetSupportTicketByIdQueryHandler(
        ISupportTicketMessageRepository messageRepository,
        SupportTicketAccessPolicy accessPolicy,
        IOptions<SupportOptions> options)
    {
        _messageRepository = messageRepository;
        _accessPolicy = accessPolicy;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketDetailDto>> Handle(GetSupportTicketByIdQuery request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketDetailDto>.Failure("conflict: support is disabled");

        var ticket = await _accessPolicy.GetAccessibleTicketAsync(request.TicketId, request.ActorUserId, request.IsStaff, ct);
        if (ticket is null)
            return Result<SupportTicketDetailDto>.Failure("forbidden: access denied");

        var messages = await _messageRepository.ListByTicketAsync(ticket.Id, request.IsStaff, ct);
        return Result<SupportTicketDetailDto>.Success(ticket.ToDetailDto(messages, DateTime.UtcNow));
    }
}
