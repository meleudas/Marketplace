using FluentValidation;
using Marketplace.Application.Common.Observability;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
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
using System.Text.Json;

namespace Marketplace.Application.Support.Commands.UpdateTicketStatus;

public sealed record UpdateTicketStatusCommand(
    string ActorUserId,
    long TicketId,
    short Status,
    string Reason,
    bool IsStaff) : IRequest<Result<SupportTicketDto>>;

public sealed class UpdateTicketStatusCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    public UpdateTicketStatusCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportTicketEventRepository _eventRepository;
    private readonly SupportTicketAccessPolicy _accessPolicy;
    private readonly SupportTicketStatePolicy _statePolicy;
    private readonly SupportHelpdeskOutboxPublisher _outboxPublisher;
    private readonly IAppNotificationScheduler _notifications;
    private readonly SupportOptions _options;

    public UpdateTicketStatusCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportTicketEventRepository eventRepository,
        SupportTicketAccessPolicy accessPolicy,
        SupportTicketStatePolicy statePolicy,
        SupportHelpdeskOutboxPublisher outboxPublisher,
        IAppNotificationScheduler notifications,
        IOptions<SupportOptions> options)
    {
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _accessPolicy = accessPolicy;
        _statePolicy = statePolicy;
        _outboxPublisher = outboxPublisher;
        _notifications = notifications;
        _options = options.Value;
    }

    public async Task<Result<SupportTicketDto>> Handle(UpdateTicketStatusCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<SupportTicketDto>.Failure("conflict: support is disabled");

        var ticket = await _accessPolicy.GetAccessibleTicketAsync(request.TicketId, request.ActorUserId, request.IsStaff, ct);
        if (ticket is null)
            return Result<SupportTicketDto>.Failure("forbidden: access denied");

        if (request.IsStaff == false)
            return Result<SupportTicketDto>.Failure("forbidden: access denied");

        var next = (SupportTicketStatus)request.Status;
        var validation = _statePolicy.ValidateTransition(ticket, next);
        if (validation.IsFailure)
            return Result<SupportTicketDto>.Failure(validation.Error ?? "conflict");

        var now = DateTime.UtcNow;
        var update = ticket.UpdateStatus(next, request.ActorUserId, request.Reason, now);
        if (update.IsFailure)
            return Result<SupportTicketDto>.Failure(update.Error ?? "conflict");

        if (ticket.IsSlaBreached(now))
            MarketplaceMetrics.SupportSlaBreachTotal.Add(1);

        await _ticketRepository.UpdateAsync(ticket, ct);
        await _eventRepository.AppendAsync(
            SupportTicketEvent.Create(
                ticket.Id,
                SupportTicketEventType.StatusChanged,
                request.ActorUserId,
                request.Reason,
                new JsonBlob(JsonSerializer.Serialize(new { status = (short)next })),
                now),
            ct);

        if (Guid.TryParse(ticket.UserId, out var userGuid))
        {
            await _notifications.ScheduleAsync(
                new AppNotificationRequest
                {
                    TemplateKey = AppNotificationTemplateKeys.SupportTicketStatusChanged,
                    CorrelationId = Guid.NewGuid(),
                    Channels = AppNotificationChannelKind.InApp,
                    Audience = AppNotificationAudienceKind.User,
                    TargetUserId = userGuid,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        ticketId = ticket.Id.Value,
                        ticketNumber = ticket.TicketNumber,
                        status = (short)next
                    })
                },
                ct);
        }

        await _outboxPublisher.PublishStatusChangedAsync(ticket.Id.Value, (short)ticket.Status, ct);
        return Result<SupportTicketDto>.Success(ticket.ToDto(now));
    }
}
