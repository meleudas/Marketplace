using System.Text.RegularExpressions;
using Marketplace.Application.Chats.Options;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Policies;

public sealed class ChatContentModerationPolicy
{
    private readonly ChatsOptions _options;

    public ChatContentModerationPolicy(IOptions<ChatsOptions> options)
    {
        _options = options.Value;
    }

    public (bool Allowed, string? MatchedPattern) Evaluate(string text)
    {
        if (!_options.ModerationEnabled || _options.ProhibitedPatterns.Length == 0)
            return (true, null);

        foreach (var pattern in _options.ProhibitedPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            try
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    return (false, pattern);
            }
            catch (RegexParseException)
            {
                if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return (false, pattern);
            }
        }

        return (true, null);
    }
}
