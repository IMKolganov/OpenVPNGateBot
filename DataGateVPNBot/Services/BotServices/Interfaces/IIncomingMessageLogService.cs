using Telegram.Bot;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IIncomingMessageLogService
{
    Task Log(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken);
    Task Log(ITelegramBotClient botClient, CallbackQuery msg, CancellationToken cancellationToken);
}