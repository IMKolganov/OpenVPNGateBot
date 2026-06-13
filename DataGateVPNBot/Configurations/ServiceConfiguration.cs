using Certes;
using Microsoft.Extensions.Configuration;
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
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DataGateVPNBot.Models.Configurations.ProfilePhotoRefreshOptions>(
            configuration.GetSection(DataGateVPNBot.Models.Configurations.ProfilePhotoRefreshOptions.SectionName));
        services.AddSingleton<IKey>(_ => LetsEncryptAccountStore.LoadOrCreateAccountKey());
        
        services.AddScoped<IIncomingMessageLogService, IncomingMessageLogService>();
        services.AddScoped<ITelegramBotUserService, TelegramBotUserService>();
        services.AddScoped<ITelegramUserProfilePhotoRefreshService, TelegramUserProfilePhotoRefreshService>();
        services.AddHostedService<MonthlyProfilePhotoRefreshHostedService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IIncomingMessageLogSenderService, IncomingMessageLogSenderService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddSingleton<TelegramUpdateHandler>();
        services.AddSingleton<ITelegramSettingsService, TelegramSettingsService>();
        services.AddScoped<IOpenVpnServersService, OpenVpnServersService>();
        services.AddScoped<IOvpnFileService, OvpnFileService>();
        services.AddScoped<IXrayClientLinkBotService, XrayClientLinkBotService>();
        services.AddScoped<IVpnProfileTokenDownloadService, VpnProfileTokenDownloadService>();
        services.AddSingleton<ServerService>();

        
        services.ConfigureTelegramBotMvc();

        services.AddControllers().AddNewtonsoftJson();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
    }
}
