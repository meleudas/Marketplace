using FluentValidation;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Behavior.Services;
using Marketplace.Domain.Behavior.Entities;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Application.Common;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;

public sealed record TrackCatalogInteractionCommand(
    Guid? ActorUserId,
    string SessionId,
    short EventType,
    string Source,
    string Payload,
    bool? ConsentGranted) : IRequest<Result>;

public sealed class TrackCatalogInteractionCommandValidator : AbstractValidator<TrackCatalogInteractionCommand>
{
    public TrackCatalogInteractionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Source).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Payload).NotEmpty().MaximumLength(100_000);
    }
}

public sealed class TrackCatalogInteractionCommandHandler : IRequestHandler<TrackCatalogInteractionCommand, Result>
{
    private readonly IBehaviorEventRepository _repository;
    private readonly IOutboxWriter _outbox;
    private readonly BehaviorPayloadRedactionService _redaction;
    private readonly BehaviorAnalyticsOptions _options;

    public TrackCatalogInteractionCommandHandler(
        IBehaviorEventRepository repository,
        IOutboxWriter outbox,
        BehaviorPayloadRedactionService redaction,
        IOptions<BehaviorAnalyticsOptions> options)
    {
        _repository = repository;
        _outbox = outbox;
        _redaction = redaction;
        _options = options.Value;
    }

    public async Task<Result> Handle(TrackCatalogInteractionCommand request, CancellationToken ct)
    {
        if (!_options.BehaviorTrackingEnabled)
            return Result.Failure("conflict: behavior tracking disabled");
        if (Encoding.UTF8.GetByteCount(request.Payload) > _options.PayloadMaxBytes)
            return Result.Failure("payload too large");
        var perMinute = await _repository.CountByTypeAsync((BehaviorEventType)request.EventType, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, ct);
        if (perMinute >= _options.RateLimitPerMinute)
            return Result.Failure("rate exceeded");

        var consent = new BehaviorConsentPolicy(_options);
        if (!consent.CanTrack(request.ConsentGranted))
            return Result.Failure("forbidden: consent required");

        if (!ShouldSample(request.SessionId))
            return Result.Success();

        var type = (BehaviorEventType)request.EventType;
        var now = DateTime.UtcNow;
        var payload = _redaction.Redact(request.Payload);
        var key = BuildEventKey(request.SessionId, type, request.Source, payload);
        var duplicates = await _repository.ListRecentDuplicatesAsync(key, type, now.AddSeconds(-Math.Max(1, _options.DuplicateWindowSeconds)), ct);
        if (duplicates.Count > 0)
            return Result.Success();

        var entity = BehaviorEvent.Create(request.ActorUserId, request.SessionId, type, key, new JsonBlob(payload), request.Source, now, now);
        var saved = await _repository.AddAsync(entity, ct);
        var eventPayload = JsonSerializer.Serialize(new
        {
            messageId = DomainEventIds.ForBehaviorEvent(saved.Id.Value),
            eventId = saved.Id.Value,
            eventType = saved.EventType.ToString(),
            occurredAtUtc = saved.OccurredAtUtc,
            userId = saved.UserId,
            sessionId = saved.SessionId,
            source = saved.Source,
            schemaVersion = saved.EventVersion.Value,
            eventKey = saved.EventKey,
            payloadJson = payload
        });
        await _outbox.AppendAsync("BehaviorEvent", saved.Id.Value.ToString(), "behavior.event.ingested", eventPayload, ct);
        return Result.Success();
    }

    private bool ShouldSample(string seed)
    {
        var percent = Math.Clamp(_options.SamplingPercent, 1, 100);
        if (percent >= 100)
            return true;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return (hash[0] % 100) < percent;
    }

    private static string BuildEventKey(string sessionId, BehaviorEventType type, string source, string payload)
    {
        var data = $"{sessionId}|{(short)type}|{source}|{payload}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
