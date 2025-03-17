using System.Reflection;
using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.UnitOfWork;
using DataGateVPNBot.Models;
using DataGateVPNBot.Services.DataServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot;

namespace DataGateVPNBot.Services;

public class ErrorService : IErrorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ErrorService> _logger;

    public ErrorService( 
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        ILogger<ErrorService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    public async Task LogErrorToDatabase(Exception exception, HttpContext? context)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var errorLogRepository = unitOfWork.GetRepository<ErrorLog>();
            var source = "Unknown";
            if (context is { Request: not null })
            {
                source = context?.Request?.Path.Value ?? "Unknown";
            }

            var errorLog = new ErrorLog
            {
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                Source = source
            };

            await errorLogRepository.AddAsync(errorLog);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log error to the database.");
        }
    }

    public async Task NotifyAdminsAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramUsersService>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var admins = await telegramUsersService.GetAdminsAsync(cancellationToken);

        if (admins is { Count: 0 })
        {
            _logger.LogWarning("No admins are configured to receive error notifications.");
            return;
        }

        _logger.LogInformation($"Notifying {admins!.Count} admins about an error.");

        foreach (var admin in admins)
        {
            try
            {
                var source = "Unknown";
                if (context is { Request: not null })
                {
                    source = context?.Request?.Path.Value ?? "Unknown";
                }
                
                var errorMessage = $"🚨 *Error Notification*\n" +
                                   $"Path: `{source}`\n" +
                                   $"Message: `{exception.Message}`\n" +
                                   $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                   $"Stack Trace:\n```{exception.StackTrace}```";

                await botClient.SendMessage(admin.TelegramId, errorMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send error notification to admin with Telegram ID {admin.TelegramId}.");
            }
        }
    }
    
    public async Task NotifyAdminsAboutStartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramUsersService>();
        var admins = await telegramUsersService.GetAdminsAsync(cancellationToken);
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        if (admins is { Count: 0 })
        {
            _logger.LogWarning("Admin chat ID is not configured.");
            return;
        }
        _logger.LogInformation("Admins count: {RecordCount}", admins!.Count);
        foreach (var admin in admins)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";

            var startupMessage = $"🚀 Bot started successfully!\n" +
                                 $"Application version: {version}\n" +
                                 $"Environment: {_environment.EnvironmentName}\n" +
                                 $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            await botClient.SendMessage(admin.TelegramId, startupMessage, cancellationToken: cancellationToken);
        }
    }
}