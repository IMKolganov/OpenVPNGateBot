using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;

namespace DataGateVPNBot.Services;

public class AdminRecipientService(
    IServiceProvider serviceProvider,
    AdminRecipientStore store,
    ILogger<AdminRecipientService> logger)
    : IAdminRecipientService
{
    public async Task<IReadOnlyList<long>> GetAdminTelegramIdsAsync(
        CancellationToken cancellationToken = default,
        bool tryRefresh = false)
    {
        if (tryRefresh)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
                var admins = await telegramUsersService.GetAdminsAsync(cancellationToken);

                var ids = admins.TelegramBotAdmins?
                    .Select(a => a.TelegramId)
                    .Where(id => id > 0)
                    .ToList();

                if (ids is { Count: > 0 })
                    store.Update(ids);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Could not refresh admin list from dashboard; using cached or reserve recipients.");
            }
        }

        var recipients = store.ResolveRecipients();
        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "No admin recipients available. Configure dashboard admins or TELEGRAMBOT_FALLBACK_ADMIN_IDS.");
        }
        else if (store.IsUsingFallbackReserve)
        {
            logger.LogWarning(
                "Using TELEGRAMBOT_FALLBACK_ADMIN_IDS reserve recipients because dashboard admin cache is empty.");
        }

        return recipients;
    }
}
