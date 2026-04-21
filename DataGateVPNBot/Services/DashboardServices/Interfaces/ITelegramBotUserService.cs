using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface ITelegramBotUserService
{
    Task<UsersResponse> RegisterUserAsync(RegisterUserFromTgBotRequest request, 
        CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken);
}