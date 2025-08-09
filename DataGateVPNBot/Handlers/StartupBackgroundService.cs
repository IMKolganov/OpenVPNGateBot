using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Services.TelegramApi;

namespace DataGateVPNBot.Handlers;

public class StartupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<StartupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return;
        await WaitForServerAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();

            try
            {
                logger.LogInformation("Attempting to notify admins and check webhook...");

                await errorService.NotifyAdminsAboutStartAsync(stoppingToken);

                if (!await webhookService.IsWebhookSetAsync(stoppingToken))
                {
                    try
                    {
                        logger.LogWarning("Webhook is not set. Attempting to set...");
                        await webhookService.DeleteWebhookAsync(stoppingToken);
                        await webhookService.SetWebhookAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await errorService.NotifyAdminsAboutExceptionAsync(ex, null, stoppingToken);
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
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    logger.LogWarning("Startup retry loop cancelled.");
                    break;
                }
            }
        }
    }

    private async Task WaitForServerAsync(CancellationToken token)
    {
        using var http = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response = await http.GetAsync("http://localhost/.well-known/healthcheck", token);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { /* ignore */ }

            await Task.Delay(1000, token);
        }

        throw new Exception("Server didn't start within timeout.");
    }
}
