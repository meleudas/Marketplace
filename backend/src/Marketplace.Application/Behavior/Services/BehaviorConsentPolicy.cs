using Marketplace.Application.Behavior.Options;

namespace Marketplace.Application.Behavior.Services;

public sealed class BehaviorConsentPolicy
{
    private readonly BehaviorAnalyticsOptions _options;

    public BehaviorConsentPolicy(BehaviorAnalyticsOptions options)
    {
        _options = options;
    }

    public bool CanTrack(bool? consent)
    {
        if (!_options.RequireConsent)
            return true;
        return consent == true;
    }
}
