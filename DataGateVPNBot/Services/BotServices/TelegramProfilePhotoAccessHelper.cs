using System.Net;
using Telegram.Bot.Exceptions;

namespace DataGateVPNBot.Services.BotServices;

internal static class TelegramProfilePhotoAccessHelper
{
    /// <summary>
    /// True when Telegram indicates we cannot talk to this user (blocked bot, deleted user, bad id, etc.).
    /// </summary>
    public static bool IsUserUnavailableForBot(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is ApiRequestException api && IsUserUnavailable(api))
                return true;
        }

        return false;
    }

    private static bool IsUserUnavailable(ApiRequestException ex)
    {
        if (ex.HttpStatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            return true;

        var m = ex.Message ?? "";

        // Telegram Bot API numeric error_code (often 403 when user blocked the bot)
        if (ex.ErrorCode is 403 or 404)
            return true;
        if (ex.ErrorCode == 400 && m.Contains("USER_ID_INVALID", StringComparison.OrdinalIgnoreCase))
            return true;

        return m.Contains("blocked by the user", StringComparison.OrdinalIgnoreCase)
               || m.Contains("bot was blocked", StringComparison.OrdinalIgnoreCase)
               || m.Contains("USER_IS_DELETED", StringComparison.OrdinalIgnoreCase)
               || m.Contains("user is deactivated", StringComparison.OrdinalIgnoreCase)
               || m.Contains("USER_ID_INVALID", StringComparison.OrdinalIgnoreCase)
               || m.Contains("PEER_ID_INVALID", StringComparison.OrdinalIgnoreCase)
               || m.Contains("CHAT_NOT_FOUND", StringComparison.OrdinalIgnoreCase);
    }
}
