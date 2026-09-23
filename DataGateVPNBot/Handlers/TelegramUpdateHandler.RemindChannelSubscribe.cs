using System.Security.Authentication;
using System.Text;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> AdminRemindChannelSubscribeAsync(
        Message msg,
        string? argument,
        CancellationToken cancellationToken)
        => await AdminRemindChannelAsync(
            msg,
            msg.From,
            argument,
            FreeTierChannelSubscribeRemindChannel.Telegram,
            cancellationToken);

    private async Task<Message> AdminRemindChannelEmailAsync(
        Message msg,
        string? argument,
        CancellationToken cancellationToken)
        => await AdminRemindChannelAsync(
            msg,
            msg.From,
            argument,
            FreeTierChannelSubscribeRemindChannel.Email,
            cancellationToken);

    private async Task<Message> AdminRemindChannelEmailFromCallbackAsync(
        Message chatMessage,
        User admin,
        string? argument,
        CancellationToken cancellationToken)
        => await AdminRemindChannelAsync(
            chatMessage,
            admin,
            argument,
            FreeTierChannelSubscribeRemindChannel.Email,
            cancellationToken);

    private async Task<Message> AdminRemindChannelSubscribeFromCallbackAsync(
        Message chatMessage,
        User admin,
        string? argument,
        CancellationToken cancellationToken)
        => await AdminRemindChannelAsync(
            chatMessage,
            admin,
            argument,
            FreeTierChannelSubscribeRemindChannel.Telegram,
            cancellationToken);

    private async Task<Message> AdminRemindChannelAsync(
        Message msg,
        User? admin,
        string? argument,
        FreeTierChannelSubscribeRemindChannel channel,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tgUserService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();

        var adminId = admin?.Id ?? 0;
        if (adminId <= 0 ||
            !await tgUserService.IsTelegramDashboardAdminAsync(adminId, cancellationToken))
        {
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "⛔ This command is only for bot administrators.",
                cancellationToken: cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(argument))
        {
            var usage = channel == FreeTierChannelSubscribeRemindChannel.Email
                ? "Usage: /remind_channel_email <userId|all>\nExample: /remind_channel_email 150"
                : "Usage: /remind_channel_subscribe <userId|telegramId|all>\nExample: /remind_channel_subscribe 22";
            return await _botClient.SendMessage(
                msg.Chat.Id,
                usage,
                cancellationToken: cancellationToken);
        }

        if (argument.Trim().Equals(BotCommands.RemindAllTarget, StringComparison.OrdinalIgnoreCase))
            return await AdminRemindChannelAllAsync(msg, channel, scope.ServiceProvider, cancellationToken);

        try
        {
            var remindService = scope.ServiceProvider.GetRequiredService<IFreeTierChannelSubscribeRemindBotService>();
            var result = await remindService.RemindAsync(argument, channel, cancellationToken);
            var prefix = result.Success ? "" : "❌ ";
            return await _botClient.SendMessage(
                msg.Chat.Id,
                prefix + result.Message,
                cancellationToken: cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Admin channel-subscribe remind: authentication failed");
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Dashboard authentication failed. Try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin channel-subscribe remind failed channel={Channel}", channel);
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Failed to send reminder. Check backend logs.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<Message> AdminRemindChannelAllAsync(
        Message msg,
        FreeTierChannelSubscribeRemindChannel channel,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var channelLabel = channel == FreeTierChannelSubscribeRemindChannel.Email ? "Email" : "TG";

        try
        {
            var digestService = services.GetRequiredService<IFreeTierUnsubscribedVpnDigestBotService>();
            var digest = await digestService.GetDigestAsync(cancellationToken);
            var targets = GetRemindAllTargets(digest?.Candidates, channel);

            if (targets.Count == 0)
            {
                return await _botClient.SendMessage(
                    msg.Chat.Id,
                    $"No digest candidates with {channelLabel} contact to remind.",
                    cancellationToken: cancellationToken);
            }

            await _botClient.SendMessage(
                msg.Chat.Id,
                $"⏳ Sending {channelLabel} reminders to {targets.Count} user(s)…",
                cancellationToken: cancellationToken);

            var remindService = services.GetRequiredService<IFreeTierChannelSubscribeRemindBotService>();
            var ok = 0;
            var failed = new List<string>();

            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await remindService.RemindAsync(target, channel, cancellationToken);
                    if (result.Success)
                        ok++;
                    else
                        failed.Add($"#{target}: {result.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Bulk channel-subscribe remind failed for {Target} channel={Channel}",
                        target,
                        channel);
                    failed.Add($"#{target}: {ex.Message}");
                }
            }

            var summary = FormatRemindAllSummary(channelLabel, targets.Count, ok, failed);
            return await _botClient.SendMessage(
                msg.Chat.Id,
                summary,
                cancellationToken: cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Admin channel-subscribe remind all: authentication failed");
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Dashboard authentication failed. Try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin channel-subscribe remind all failed channel={Channel}", channel);
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "❌ Failed to send reminders. Check backend logs.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>Builds the admin summary after a bulk remind. Public for unit tests.</summary>
    public static string FormatRemindAllSummary(
        string channelLabel,
        int total,
        int ok,
        IReadOnlyList<string> failed)
    {
        var sb = new StringBuilder();
        sb.Append(ok == total ? "✅ " : "⚠️ ");
        sb.Append(channelLabel);
        sb.Append(" reminders: ");
        sb.Append(ok);
        sb.Append('/');
        sb.Append(total);
        sb.Append(" sent.");

        if (failed.Count == 0)
            return sb.ToString();

        sb.Append("\n\nFailed:\n");
        const int maxLines = 15;
        foreach (var line in failed.Take(maxLines))
            sb.Append("• ").Append(line).Append('\n');
        if (failed.Count > maxLines)
            sb.Append("• …and ").Append(failed.Count - maxLines).Append(" more");

        var text = sb.ToString().TrimEnd();
        return text.Length > 4090 ? text[..4090] + "…" : text;
    }
}
