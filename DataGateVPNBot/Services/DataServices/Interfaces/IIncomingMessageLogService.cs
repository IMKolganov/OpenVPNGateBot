using Telegram.Bot;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.DataServices.Interfaces;

public interface IIncomingMessageLogService
{
    Task Log(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken);
}