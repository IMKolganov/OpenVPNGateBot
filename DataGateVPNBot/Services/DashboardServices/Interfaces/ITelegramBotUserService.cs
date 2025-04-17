using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotUser.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface ITelegramBotUserService
{
    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, 
        CancellationToken cancellationToken);

    Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken);
}