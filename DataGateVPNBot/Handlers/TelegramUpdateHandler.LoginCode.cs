using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private static readonly string[] LoginCodeTextTriggers =
    [
        "дай код",
        "дайте код",
        "give code",
        "login code",
    ];

    private static bool IsLoginCodeTextRequest(string messageText)
    {
        var normalized = messageText.Trim().ToLowerInvariant();
        return LoginCodeTextTriggers.Any(t => normalized == t || normalized.StartsWith(t + " ", StringComparison.Ordinal));
    }

    private async Task<Message> SendDashboardLoginCodeAsync(Message msg, CancellationToken cancellationToken)
    {
        if (msg.From is null)
            return msg;

        var result = await authService.RequestDashboardLoginCodeAsync(msg.From.Id, cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Code))
        {
            return await _botClient.SendMessage(
                msg.Chat,
                "Could not issue a login code. Register in the bot with /register first, or contact support if you are blocked.",
                cancellationToken: cancellationToken);
        }

        var minutes = Math.Max(1, result.ExpiresInSeconds / 60);
        var text =
            $"Your dashboard login code:\n\n" +
            $"<code>{result.Code}</code>\n\n" +
            $"Valid for {minutes} min. Enter it on the DataGate Monitor sign-in page under «Continue with Telegram».\n" +
            "Do not share this code.";

        return await _botClient.SendMessage(
            msg.Chat,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
