using DataGateVPNBot.Handlers;
using DataGateVPNBot.Services;
using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DataServices;
using DataGateVPNBot.Services.DataServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;

namespace DataGateVPNBot.Configurations;

public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<IIncomingMessageLogService, IncomingMessageLogService>();
        services.AddScoped<ITelegramUsersService, TelegramUsersService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddSingleton<TelegramUpdateHandler>();
        services.AddSingleton<ITelegramSettingsService, TelegramSettingsService>();
        services.AddScoped<IOvpnFileService, OvpnFileService>();
        
        services.ConfigureTelegramBotMvc();

        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
    }
}
