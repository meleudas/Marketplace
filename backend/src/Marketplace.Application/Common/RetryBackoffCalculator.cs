namespace Marketplace.Application.Common;

public static class RetryBackoffCalculator
{
    public static DateTime ComputeNextAttemptUtc(
        int attemptsAfterFailure,
        int baseBackoffMinutes,
        int maxBackoffMinutes,
        DateTime utcNow)
    {
        var exponent = Math.Min(attemptsAfterFailure, 6);
        var delayMinutes = Math.Min(maxBackoffMinutes, baseBackoffMinutes * Math.Pow(2, exponent));
        var jitterSeconds = Random.Shared.Next(1, 15);
        return utcNow.AddMinutes(delayMinutes).AddSeconds(jitterSeconds);
    }
}
