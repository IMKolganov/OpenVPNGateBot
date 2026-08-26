using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot;

namespace DataGateVPNBot.Services;

public class DashboardAuthAlertService(
    AdminRecipientStore store,
    ITelegramBotClient botClient,
    ILogger<DashboardAuthAlertService> logger)
    : IDashboardAuthAlertService
{
    private readonly object _lock = new();
    private DateTime _lastAlertUtc = DateTime.MinValue;
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromHours(1);

    public async Task TryNotifyAuthFailureAsync(string reason, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastAlertUtc < AlertCooldown)
            {
                logger.LogDebug("Skipping dashboard auth alert (cooldown active).");
                return;
            }

            _lastAlertUtc = DateTime.UtcNow;
        }

        var recipients = store.ResolveRecipients();
        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "Dashboard auth failed but no admin recipients configured (cache empty, no TELEGRAMBOT_FALLBACK_ADMIN_IDS): {Reason}",
                reason);
            return;
        }

        var reserveNote = store.IsUsingFallbackReserve
            ? "\n_(notifying reserve TELEGRAMBOT_FALLBACK_ADMIN_IDS — dashboard admin cache is empty)_"
            : string.Empty;

        var message =
            "⚠️ *Dashboard API authentication failed*\n" +
            $"Reason: `{TrimReason(reason)}`\n" +
            "The bot cannot talk to the dashboard (API key revoked, wrong secret, or rate limit).\n" +
            "User commands like /register will not work until credentials are fixed in Application Settings." +
            reserveNote +
            $"\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        logger.LogWarning(
            "Sending dashboard auth failure alert to {Count} recipient(s). UsingReserve={UsingReserve}",
            recipients.Count,
            store.IsUsingFallbackReserve);

        foreach (var adminId in recipients)
        {
            try
            {
                await botClient.SendMessage(
                    adminId,
                    message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send dashboard auth alert to Telegram ID {TelegramId}.",
                    adminId);
            }
        }
    }

    private static string TrimReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return "Unknown";

        return reason.Length > 500 ? reason[..497] + "..." : reason;
    }
}
