using DataGateVPNBot.Models;

namespace DataGateVPNBot.Services.DataServices.Interfaces;

public interface ITelegramUsersService
{
    Task RegisterUserAsync(long telegramId, string? username, string? firstName, string? lastName, 
        CancellationToken cancellationToken);
    Task<List<TelegramUser>?> GetAdminsAsync(CancellationToken cancellationToken);
}