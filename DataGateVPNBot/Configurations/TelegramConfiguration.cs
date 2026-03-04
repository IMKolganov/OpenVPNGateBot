using DataGateVPNBot.Extensions;
using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.LetsEncrypt;
using DataGateVPNBot.Services.TelegramApi;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace DataGateVPNBot.Configurations;

public static class TelegramConfiguration
{
    public static void ConfigureTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration section to BotConfiguration with default values
        services.Configure<BotConfiguration>(configuration.GetSection("BotConfiguration"));

        // Override with environment variables
        services.PostConfigure<BotConfiguration>(TelegramConfigurationHelper.ApplyEnvAndValidate);

        // Register TelegramBotClient using options
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
            return new TelegramBotClient(options.BotToken);
        });

        services.AddSingleton<OpensslCertificateGenerator>();
        services.AddTransient<LetsEncryptCertificateGenerator>();
        services.AddHttpClient<WebhookService>();
        services.AddHostedService<StartupBackgroundService>();
    }
}
