using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface ITelegramBotUserService
{
    Task<UsersResponse> RegisterUserAsync(RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken);
}