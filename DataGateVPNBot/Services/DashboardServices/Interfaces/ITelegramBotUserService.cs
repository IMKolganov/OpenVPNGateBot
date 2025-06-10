using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface ITelegramBotUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, 
        CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken);
}