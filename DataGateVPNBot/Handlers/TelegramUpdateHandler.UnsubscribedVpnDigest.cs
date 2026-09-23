using System.Security.Authentication;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> AdminUnsubscribedVpnUsersDigestAsync(
        Message msg,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tgUserService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();

        if (!await tgUserService.IsTelegramDashboardAdminAsync(msg.From!.Id, cancellationToken))
        {
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "⛔ This command is only for bot administrators.",
                cancellationToken: cancellationToken);
        }

        await _botClient.SendMessage(
            msg.Chat.Id,
            "⏳ Building Free/Default unsubscribed VPN digest…",
            cancellationToken: cancellationToken);

        try
        {
            var digestService = scope.ServiceProvider.GetRequiredService<IFreeTierUnsubscribedVpnDigestBotService>();
            var digest = await digestService.GetDigestAsync(cancellationToken);

            if (digest is null || string.IsNullOrWhiteSpace(digest.Text))
            {
                return await _botClient.SendMessage(
                    msg.Chat.Id,
                    "Could not load the digest from the dashboard API.",
                    cancellationToken: cancellationToken);
            }

            var liveText = digest.Text.StartsWith("📋", StringComparison.Ordinal) ||
                           digest.Text.StartsWith("📅", StringComparison.Ordinal)
                ? "🔎 Live snapshot\n" + digest.Text
                : "🔎 Live snapshot\n\n" + digest.Text;

            if (liveText.Length > 4090)
                liveText = liveText[..4090] + "…";

            var keyboard = BuildRemindKeyboard(digest.Candidates);
            return await _botClient.SendMessage(
                msg.Chat.Id,
                liveText,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Admin unsubscribed VPN digest: authentication failed");
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Dashboard authentication failed. Try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin unsubscribed VPN digest failed");
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Failed to load the digest. Check backend logs.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Optional first row: TG all / Email all. Then one row per candidate (up to 20): TG and/or Email.
    /// </summary>
    public static InlineKeyboardMarkup? BuildRemindKeyboard(
        IReadOnlyList<FreeTierEnforcementCandidateDto>? candidates)
    {
        if (candidates is null || candidates.Count == 0)
            return null;

        var actionable = candidates
            .Where(c => c.TelegramId is > 0 || !string.IsNullOrWhiteSpace(c.Email))
            .OrderBy(c => c.DisplayName)
            .ToList();
        if (actionable.Count == 0)
            return null;

        var rows = new List<InlineKeyboardButton[]>();

        var allButtons = new List<InlineKeyboardButton>(2);
        if (actionable.Any(c => c.TelegramId is > 0))
        {
            allButtons.Add(InlineKeyboardButton.WithCallbackData(
                "TG all",
                $"{BotCommands.CommandRemindChannelSubscribe} {BotCommands.RemindAllTarget}"));
        }

        if (actionable.Any(c => !string.IsNullOrWhiteSpace(c.Email)))
        {
            allButtons.Add(InlineKeyboardButton.WithCallbackData(
                "Email all",
                $"{BotCommands.CommandRemindChannelEmail} {BotCommands.RemindAllTarget}"));
        }

        if (allButtons.Count > 0)
            rows.Add(allButtons.ToArray());

        foreach (var c in actionable.Take(20))
        {
            var buttons = new List<InlineKeyboardButton>(2);
            if (c.TelegramId is > 0)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData(
                    $"TG #{c.UserId}",
                    $"{BotCommands.CommandRemindChannelSubscribe} {c.UserId}"));
            }

            if (!string.IsNullOrWhiteSpace(c.Email))
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData(
                    $"Email #{c.UserId}",
                    $"{BotCommands.CommandRemindChannelEmail} {c.UserId}"));
            }

            if (buttons.Count > 0)
                rows.Add(buttons.ToArray());
        }

        return rows.Count == 0 ? null : new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Dashboard userIds to remind for a bulk "all" action (every actionable digest candidate).
    /// </summary>
    public static IReadOnlyList<string> GetRemindAllTargets(
        IReadOnlyList<FreeTierEnforcementCandidateDto>? candidates,
        FreeTierChannelSubscribeRemindChannel channel)
    {
        if (candidates is null || candidates.Count == 0)
            return [];

        IEnumerable<FreeTierEnforcementCandidateDto> filtered = channel switch
        {
            FreeTierChannelSubscribeRemindChannel.Email =>
                candidates.Where(c => !string.IsNullOrWhiteSpace(c.Email)),
            _ =>
                candidates.Where(c => c.TelegramId is > 0),
        };

        return filtered
            .OrderBy(c => c.DisplayName)
            .Select(c => c.UserId.ToString())
            .Distinct()
            .ToList();
    }

    [Obsolete("Use BuildRemindKeyboard")]
    public static InlineKeyboardMarkup? BuildEmailRemindKeyboard(
        IReadOnlyList<FreeTierEnforcementCandidateDto>? candidates)
        => BuildRemindKeyboard(candidates);
}
