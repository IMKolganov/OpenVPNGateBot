using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Configurations;

public static class TelegramStarsConfigurationHelper
{
    public static void ApplyEnv(TelegramStarsConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var enabled = Environment.GetEnvironmentVariable("STARS_ENABLED");
        if (bool.TryParse(enabled, out var enabledValue))
            config.Enabled = enabledValue;

        var amounts = Environment.GetEnvironmentVariable("STARS_AMOUNTS");
        if (!string.IsNullOrWhiteSpace(amounts))
            config.Amounts = amounts.Trim();
    }

    public static IReadOnlyList<int> ParseAmounts(string? amounts)
    {
        if (string.IsNullOrWhiteSpace(amounts))
            return [];

        return amounts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }
}
