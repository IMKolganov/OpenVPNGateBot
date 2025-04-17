using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Services.TelegramApi;

namespace DataGateVPNBot.Handlers;

public class StartupNotificationHandler : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupNotificationHandler> _logger;

    public StartupNotificationHandler(IServiceProvider serviceProvider, ILogger<StartupNotificationHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();

        await errorService.NotifyAdminsAboutStartAsync(cancellationToken);

        if (!await webhookService.IsWebhookSetAsync(cancellationToken))
        {
            _logger.LogWarning("Webhook is not set. Attempting to set...");
            await webhookService.DeleteWebhookAsync(cancellationToken);
            await webhookService.SetWebhookAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
