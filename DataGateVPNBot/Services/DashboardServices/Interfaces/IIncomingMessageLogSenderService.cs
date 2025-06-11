using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface IIncomingMessageLogSenderService
{
    Task<AddMessageResponse> TelegramBotIncomingMessageLogAddMessageAsync(AddMessageRequest request,
        CancellationToken cancellationToken);
}