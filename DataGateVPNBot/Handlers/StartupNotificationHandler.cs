using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Services.TelegramApi;

namespace DataGateVPNBot.Handlers;

public class StartupNotificationHandler(IServiceProvider serviceProvider, ILogger<StartupNotificationHandler> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();

            try
            {
                logger.LogInformation("Attempting to notify admins and check webhook...");

                await errorService.NotifyAdminsAboutStartAsync(cancellationToken);

                if (!await webhookService.IsWebhookSetAsync(cancellationToken))
                {
                    try
                    {
                        logger.LogWarning("Webhook is not set. Attempting to set...");
                        await webhookService.DeleteWebhookAsync(cancellationToken);
                        await webhookService.SetWebhookAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                    }

                }

                logger.LogInformation("Startup initialization completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Startup task failed. Retrying in 10 seconds...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    logger.LogWarning("Startup retry loop cancelled.");
                    break;
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}