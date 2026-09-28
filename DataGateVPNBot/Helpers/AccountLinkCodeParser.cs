namespace DataGateVPNBot.Helpers;

public static class AccountLinkCodeParser
{
    public const string StartPayloadPrefix = "link_";

    public static bool TryExtract(string messageText, out string code)
    {
        code = string.Empty;
        var trimmed = messageText.Trim();
        if (trimmed.StartsWith('/'))
            return false;

        return TryNormalizeToken(trimmed, out code);
    }

    /// <summary>
    /// Parses <c>/start link_ABCD2345</c> deep-link payloads from Telegram.
    /// </summary>
    public static bool TryParseStartPayload(string? argument, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(argument))
            return false;

        var token = argument.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        if (!token.StartsWith(StartPayloadPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return TryNormalizeToken(token[StartPayloadPrefix.Length..], out code);
    }

    public static string BuildStartPayload(string code)
        => StartPayloadPrefix + code.Trim().ToUpperInvariant();

    public static bool TryNormalizeToken(string? value, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var token = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        if (token.Length != 8)
            return false;

        foreach (var ch in token)
        {
            if (!IsCodeChar(ch))
                return false;
        }

        code = token.ToUpperInvariant();
        return true;
    }

    public static bool IsCodeChar(char ch)
        => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '2' and <= '9';

    public static string RedactSensitiveMessageText(string? messageText)
    {
        if (string.IsNullOrEmpty(messageText))
            return string.Empty;

        if (TryExtract(messageText, out _))
            return "[account-link-code-redacted]";

        if (messageText.StartsWith("/link_account", StringComparison.OrdinalIgnoreCase))
        {
            var parts = messageText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && TryNormalizeToken(parts[1], out _))
                return "/link_account [redacted]";
        }

        if (messageText.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            var parts = messageText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && TryParseStartPayload(parts[1], out _))
                return "/start link_[redacted]";
        }

        return messageText;
    }
}
