namespace DataGateVPNBot.Handlers;

public static class TelegramCopyTextHelper
{
    public static string? TryGetVlessCopyText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var line = raw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith("vless://", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(line))
            return null;

        // Telegram copy_text payload is length-limited; avoid API rejection.
        return line.Length <= 256 ? line : null;
    }
}
