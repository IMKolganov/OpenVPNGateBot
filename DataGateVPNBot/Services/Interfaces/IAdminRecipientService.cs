namespace DataGateVPNBot.Services.Interfaces;

public interface IAdminRecipientService
{
    /// <summary>
    /// Returns admin Telegram IDs, refreshing from dashboard when possible and falling back to cache/env.
    /// </summary>
    Task<IReadOnlyList<long>> GetAdminTelegramIdsAsync(
        CancellationToken cancellationToken = default,
        bool tryRefresh = false);
}
