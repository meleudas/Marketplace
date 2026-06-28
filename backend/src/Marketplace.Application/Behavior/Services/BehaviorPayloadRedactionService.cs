using System.Text.RegularExpressions;

namespace Marketplace.Application.Behavior.Services;

public sealed class BehaviorPayloadRedactionService
{
    private static readonly Regex EmailRegex = new(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\+?\d[\d\s\-\(\)]{7,}", RegexOptions.Compiled);
    private static readonly Regex JwtRegex = new(@"eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*", RegexOptions.Compiled);

    public string Redact(string payload)
    {
        var result = EmailRegex.Replace(payload, "[redacted_email]");
        result = PhoneRegex.Replace(result, "[redacted_phone]");
        result = JwtRegex.Replace(result, "[redacted_jwt]");
        return result;
    }
}
