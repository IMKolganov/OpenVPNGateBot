using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Configurations;

public static class CryptoPayConfigurationHelper
{
    public static void ApplyEnv(CryptoPayConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var enabled = Environment.GetEnvironmentVariable("CRYPTOPAY_ENABLED");
        if (bool.TryParse(enabled, out var enabledValue))
            config.Enabled = enabledValue;

        var token = Environment.GetEnvironmentVariable("CRYPTOPAY_API_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            config.ApiToken = token.Trim();

        var testnet = Environment.GetEnvironmentVariable("CRYPTOPAY_USE_TESTNET");
        if (bool.TryParse(testnet, out var useTestnet))
            config.UseTestnet = useTestnet;

        var fiat = Environment.GetEnvironmentVariable("CRYPTOPAY_FIAT");
        if (!string.IsNullOrWhiteSpace(fiat))
            config.Fiat = fiat.Trim().ToUpperInvariant();

        var amounts = Environment.GetEnvironmentVariable("CRYPTOPAY_AMOUNTS");
        if (!string.IsNullOrWhiteSpace(amounts))
            config.Amounts = amounts.Trim();

        var assets = Environment.GetEnvironmentVariable("CRYPTOPAY_ACCEPTED_ASSETS");
        if (!string.IsNullOrWhiteSpace(assets))
            config.AcceptedAssets = assets.Trim();

        var swapTo = Environment.GetEnvironmentVariable("CRYPTOPAY_SWAP_TO");
        if (swapTo != null)
            config.SwapTo = swapTo.Trim();

        var expires = Environment.GetEnvironmentVariable("CRYPTOPAY_INVOICE_EXPIRES_IN");
        if (int.TryParse(expires, out var expiresIn) && expiresIn > 0)
            config.InvoiceExpiresInSeconds = expiresIn;
    }

    public static IReadOnlyList<decimal> ParseAmounts(string? amounts)
    {
        if (string.IsNullOrWhiteSpace(amounts))
            return [];

        return amounts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => decimal.TryParse(part, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value
                : 0m)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }
}
