namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface ITelegramUserProfilePhotoRefreshService
{
    /// <summary>
    /// For every Telegram bot user in the dashboard DB, fetches the current profile photo from Telegram
    /// and upserts it via <c>POST api/tgbot-users/profile-photo</c>.
    /// </summary>
    Task<ProfilePhotoBatchRefreshResult> RefreshAllFromTelegramAsync(CancellationToken cancellationToken);
}
