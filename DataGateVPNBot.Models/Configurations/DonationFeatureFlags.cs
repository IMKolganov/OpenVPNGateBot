namespace DataGateVPNBot.Models.Configurations;

/// <summary>
/// Shared kill-switch helpers for voluntary donations. Both channels default to off;
/// enable explicitly via config/env (<c>STARS_ENABLED</c> / <c>CRYPTOPAY_ENABLED</c>).
/// </summary>
public static class DonationFeatureFlags
{
    public static bool IsStarsLive(TelegramStarsConfiguration stars) =>
        stars.IsConfigured;

    public static bool IsCryptoPayLive(CryptoPayConfiguration crypto) =>
        crypto.IsConfigured;

    /// <summary>True when at least one donation channel can offer a payment UI.</summary>
    public static bool IsAnyDonationChannelLive(
        TelegramStarsConfiguration stars,
        CryptoPayConfiguration crypto) =>
        IsStarsLive(stars) || IsCryptoPayLive(crypto);
}
