using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface IIncomingMessageLogSenderService
{
    Task<AddMessageResponse> TelegramBotIncomingMessageLogAddMessageAsync(AddMessageRequest request,
        CancellationToken cancellationToken);
}