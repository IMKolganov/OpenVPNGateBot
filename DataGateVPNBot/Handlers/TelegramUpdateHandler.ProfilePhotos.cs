using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> AdminRefreshAllProfilePhotosAsync(Message msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tgUserService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var refresh = scope.ServiceProvider.GetRequiredService<ITelegramUserProfilePhotoRefreshService>();

        if (!await tgUserService.IsTelegramDashboardAdminAsync(msg.From!.Id, cancellationToken))
        {
            return await _botClient.SendMessage(
                msg.Chat.Id,
                "⛔ This command is only for bot administrators.",
                cancellationToken: cancellationToken);
        }

        await _botClient.SendMessage(
            msg.Chat.Id,
            "⏳ Syncing profile photos for all registered users with the dashboard. This may take a while…",
            cancellationToken: cancellationToken);

        var result = await refresh.RefreshAllFromTelegramAsync(cancellationToken);
        var text = FormatProfilePhotoRefreshReport(result);

        if (text.Length > 4090)
            text = text[..4090] + "…";

        return await _botClient.SendMessage(
            msg.Chat.Id,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private static string FormatProfilePhotoRefreshReport(ProfilePhotoBatchRefreshResult r)
    {
        var lines = new List<string>
        {
            "<b>Profile photo sync</b>",
            $"Users in DB: {r.TotalUsers}",
            $"Avatars <b>updated</b> in DB: {r.Updated}",
            $"Unchanged (same Telegram file): {r.Unchanged}",
            $"No profile photo on Telegram: {r.SkippedNoProfilePhoto}",
            $"No access (blocked bot / deleted / invalid id): {r.SkippedUserUnavailable}",
            $"Failed (other): {r.Failed}"
        };

        if (r.Errors.Count > 0)
        {
            lines.Add("");
            lines.Add("<b>Error samples</b>");
            foreach (var e in r.Errors)
                lines.Add(System.Net.WebUtility.HtmlEncode(e));
        }

        return string.Join("\n", lines);
    }
}
