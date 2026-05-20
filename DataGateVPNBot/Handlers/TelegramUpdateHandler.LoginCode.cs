using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> SendDashboardLoginCodeAsync(Message msg, CancellationToken cancellationToken)
    {
        if (msg.From is null)
            return msg;

        var telegramId = msg.From.Id;
        var result = await authService.RequestDashboardLoginCodeAsync(telegramId, cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Code))
        {
            return await _botClient.SendMessage(
                msg.Chat,
                await GetLocalizationTextAsync("DashboardLoginCodeError", telegramId, cancellationToken),
                cancellationToken: cancellationToken);
        }

        var minutes = Math.Max(1, result.ExpiresInSeconds / 60);
        var text = await GetLocalizationTextAsync(
            "DashboardLoginCode",
            telegramId,
            new Dictionary<string, string>
            {
                ["code"] = result.Code,
                ["minutes"] = minutes.ToString(),
            },
            cancellationToken);

        return await _botClient.SendMessage(
            msg.Chat,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
