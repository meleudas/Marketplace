using FluentValidation;
using Marketplace.Application.Support;
using Marketplace.Application.Support.DTOs;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Marketplace.Domain.Support.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Commands.CreateSupportTicket;

public sealed record CreateSupportTicketCommand(
    string ActorUserId,
    string Subject,
    string Message,
    short Priority,
    long? OrderId,
    Guid? CompanyId,
    long? CategoryId) : IRequest<Result<SupportTicketDto>>;

public sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}

public sealed class CreateSupportTicketCommandHandler : IRequestHandler<CreateSupportTicketCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly ISupportExternalLinkRepository _externalLinkRepository;
    private readonly SupportAntiAbusePolicy _antiAbuse;
    private readonly SupportEscalationPolicy _slaPolicy;
    private readonly SupportHelpdeskOutboxPublisher _outboxPublisher;
    private readonly SupportOptions _options;

    public CreateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportTicketEventRepository eventRepository,
        ISupportExternalLinkRepository externalLinkRepository,
        SupportAntiAbusePolicy antiAbuse,
        SupportEscalationPolicy slaPolicy,
        SupportHelpdeskOutboxPublisher outboxPublisher,
        IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _externalLinkRepository = externalLinkRepository;
        _antiAbuse = antiAbuse;
        _slaPolicy = slaPolicy;
        _outboxPublisher = outboxPublisher;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketDto>> Handle(CreateSupportTicketCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketDto>.Failure("conflict: support is disabled");

        var abuse = await _antiAbuse.EvaluateCreateAsync(request.ActorUserId, ct);
        if (!abuse.Allowed)
            return Result<SupportTicketDto>.Failure(abuse.Reason ?? "rate exceeded");

        var now = DateTime.UtcNow;
        var priority = (SupportTicketPriority)request.Priority;
        var ticket = SupportTicket.Create(
            GenerateTicketNumber(),
            request.ActorUserId,
            request.Subject,
            request.Message,
            priority,
            request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null,
            request.CompanyId.HasValue ? CompanyId.From(request.CompanyId.Value) : null,
            request.CategoryId.HasValue ? CategoryId.From(request.CategoryId.Value) : null,
            _slaPolicy.ComputeSlaDueAt(priority, now),
            now);

        var saved = await _ticketRepository.AddAsync(ticket, ct);
        await _eventRepository.AppendAsync(
            SupportTicketEvent.Create(
                saved.Id,
                SupportTicketEventType.Created,
                request.ActorUserId,
                "created",
                new JsonBlob("{}"),
                now),
            ct);

        if (_options.HelpdeskSyncEnabled)
        {
            await _externalLinkRepository.UpsertAsync(
                SupportExternalLink.CreatePending(saved.Id, _options.HelpdeskProvider, now),
                ct);
            await _outboxPublisher.PublishTicketCreatedAsync(saved.Id.Value, ct);
        }

        return Result<SupportTicketDto>.Success(saved.ToDto(now));
    }

    private static string GenerateTicketNumber() =>
        $"SUP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
}
