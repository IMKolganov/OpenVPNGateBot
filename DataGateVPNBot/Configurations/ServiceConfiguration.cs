using Certes;
using DataGateVPNBot.Handlers;
using DataGateVPNBot.Services;
using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Services.LetsEncrypt;
using Telegram.Bot.AspNetCore;
using OvpnFileService = DataGateVPNBot.Services.BotServices.OvpnFileService;

namespace DataGateVPNBot.Configurations;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<IKey>(_ => LetsEncryptAccountStore.LoadOrCreateAccountKey());
        services.AddSingleton<CertificateGenerator>();
        
        services.AddScoped<IIncomingMessageLogService, IncomingMessageLogService>();
        services.AddScoped<ITelegramBotUserService, TelegramBotUserService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IIncomingMessageLogSenderService, IncomingMessageLogSenderService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddSingleton<TelegramUpdateHandler>();
        services.AddSingleton<ITelegramSettingsService, TelegramSettingsService>();
        services.AddScoped<IOpenVpnServersService, OpenVpnServersService>();
        services.AddScoped<IOvpnFileService, OvpnFileService>();
        services.AddSingleton<ServerService>();

        
        services.ConfigureTelegramBotMvc();

        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
    }
}
