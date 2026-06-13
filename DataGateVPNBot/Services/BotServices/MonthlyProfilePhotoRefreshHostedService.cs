using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DataGateVPNBot.Services.BotServices;

/// <summary>
/// Periodically syncs Telegram profile photos into the dashboard DB and notifies admins when at least one avatar was updated.
/// </summary>
public sealed class MonthlyProfilePhotoRefreshHostedService(
    IServiceProvider serviceProvider,
    IOptions<ProfilePhotoRefreshOptions> options,
    ILogger<MonthlyProfilePhotoRefreshHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.PeriodicRefreshEnabled)
        {
            logger.LogInformation("Periodic profile photo refresh is disabled (ProfilePhotoRefresh:PeriodicRefreshEnabled).");
            return;
        }

        var days = Math.Clamp(options.Value.IntervalDays, 1, 365);
        using var timer = new PeriodicTimer(TimeSpan.FromDays(days));

        logger.LogInformation(
            "Scheduled Telegram profile photo refresh every {Days} day(s). First run after this interval.",
            days);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceSafeAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<ITelegramUserProfilePhotoRefreshService>();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();

            var result = await refresh.RefreshAllFromTelegramAsync(stoppingToken);

            if (!result.AnyDatabaseChange)
            {
                logger.LogInformation(
                    "Periodic profile photo refresh finished: total={Total}, updated=0, unchanged={Unchanged}, noPhoto={NoPhoto}, unavailable={Unavailable}, failed={Failed}",
                    result.TotalUsers, result.Unchanged, result.SkippedNoProfilePhoto, result.SkippedUserUnavailable,
                    result.Failed);
                return;
            }

            var msg =
                "🖼 Scheduled profile photo sync\n" +
                $"Users in DB: {result.TotalUsers}\n" +
                $"Avatars updated in DB: {result.Updated}\n" +
                $"Unchanged (same file id): {result.Unchanged}\n" +
                $"No Telegram photo: {result.SkippedNoProfilePhoto}\n" +
                $"No access (blocked bot / etc.): {result.SkippedUserUnavailable}\n" +
                $"Failed (other): {result.Failed}\n" +
                (result.Errors.Count > 0 ? "Samples:\n" + string.Join("\n", result.Errors) : "");

            if (msg.Length > 4090)
                msg = msg[..4090] + "…";

            await errorService.SendMessageToAdminsAsync(msg, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Periodic profile photo refresh failed.");
        }
    }
}
