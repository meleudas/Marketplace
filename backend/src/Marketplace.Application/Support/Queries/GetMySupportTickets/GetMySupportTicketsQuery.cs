using FluentValidation;
using Marketplace.Application.Support;
using Marketplace.Application.Support.DTOs;
using Marketplace.Application.Support.Options;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Queries.GetMySupportTickets;

public sealed record GetMySupportTicketsQuery(string ActorUserId, int Page, int Size) : IRequest<Result<SupportTicketListDto>>;

public sealed class GetMySupportTicketsQueryValidator : AbstractValidator<GetMySupportTicketsQuery>
{
    public GetMySupportTicketsQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}

public sealed class GetMySupportTicketsQueryHandler : IRequestHandler<GetMySupportTicketsQuery, Result<SupportTicketListDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly SupportOptions _options;

    public GetMySupportTicketsQueryHandler(ISupportTicketRepository ticketRepository, IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketListDto>> Handle(GetMySupportTicketsQuery request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketListDto>.Failure("conflict: support is disabled");

        var skip = (request.Page - 1) * request.Size;
        var now = DateTime.UtcNow;
        var total = await _ticketRepository.CountByUserAsync(request.ActorUserId, ct);
        var tickets = await _ticketRepository.ListByUserAsync(request.ActorUserId, skip, request.Size, ct);
        return Result<SupportTicketListDto>.Success(new SupportTicketListDto(
            tickets.Select(x => x.ToDto(now)).ToList(),
            total,
            request.Page,
            request.Size));
    }
}
