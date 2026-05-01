using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
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

    /// <summary>Returns true if <paramref name="telegramUserId"/> is among dashboard bot admins.</summary>
    Task<bool> IsTelegramDashboardAdminAsync(long telegramUserId, CancellationToken cancellationToken);

    Task<GetAllTelegramUsersResponse?> GetAllTelegramUsersAsync(CancellationToken cancellationToken);

    Task<UpsertTelegramBotUserProfilePhotoResponse?> UpsertProfilePhotoAsync(
        UpsertTelegramBotUserProfilePhotoRequest request,
        CancellationToken cancellationToken);
}