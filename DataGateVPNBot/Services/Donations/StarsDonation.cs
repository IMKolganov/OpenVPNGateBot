using System.Globalization;

namespace DataGateVPNBot.Services.Donations;

public static class StarsDonation
{
    public const string Currency = "XTR";
    public const string CallbackPrefix = "donate:stars:";
    public const string PayloadPrefix = "stars:";

    public static string Callback(int stars) => $"{CallbackPrefix}{stars}";

    public static string Payload(long telegramId, int stars) =>
        $"{PayloadPrefix}{telegramId}:{stars}";

    public static bool TryParseCallback(string? data, out int stars)
    {
        stars = 0;
        if (string.IsNullOrWhiteSpace(data) ||
            !data.StartsWith(CallbackPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(data[CallbackPrefix.Length..], NumberStyles.Integer,
            CultureInfo.InvariantCulture, out stars) && stars > 0;
    }

    public static bool TryParsePayload(string? payload, out long telegramId, out int stars)
    {
        telegramId = 0;
        stars = 0;
        if (string.IsNullOrWhiteSpace(payload) ||
            !payload.StartsWith(PayloadPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = payload[PayloadPrefix.Length..].Split(':', 2);
        if (rest.Length != 2)
            return false;

        return long.TryParse(rest[0], out telegramId) && telegramId > 0 &&
               int.TryParse(rest[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out stars) &&
               stars > 0;
    }

    public static bool IsDonatePreCheckout(string? currency, string? payload) =>
        string.Equals(currency, Currency, StringComparison.OrdinalIgnoreCase) &&
        TryParsePayload(payload, out _, out _);
}
