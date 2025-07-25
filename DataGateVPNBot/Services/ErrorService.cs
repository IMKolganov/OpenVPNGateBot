using System.Reflection;
using DataGateVPNBot.Models;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot;

namespace DataGateVPNBot.Services;

public class ErrorService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<ErrorService> logger)
    : IErrorService
{
    public void LogErrorToDatabase(Exception exception, HttpContext? context)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            // var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // var errorLogRepository = unitOfWork.GetRepository<ErrorLog>();
        
            var source = context?.Request?.Path.Value ?? "Unknown";

            const int maxLength = 4000;
            string message = exception.Message.Length > maxLength 
                ? exception.Message.Substring(0, maxLength - 3) + "..." 
                : exception.Message;

            string stackTrace = (exception.StackTrace?.Length ?? 0) > maxLength 
                ? exception.StackTrace?.Substring(0, maxLength - 3) + "..." 
                : exception.StackTrace ?? string.Empty;

            var errorLog = new ErrorLog
            {
                Message = message,
                StackTrace = stackTrace,
                Timestamp = DateTime.UtcNow,
                Source = source
            };

            // await errorLogRepository.AddAsync(errorLog);
            // await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log error to the database.");
        }
    }
    
    public async Task SendMessageToAdminsAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var admins =  await telegramUsersService.GetAdminsAsync(cancellationToken);
        
        if (admins.TelegramBotAdmins is { Count: 0 })
        {
            logger.LogWarning("Admin chat ID is not configured.");
            return;
        }
        logger.LogInformation("Admins count: {RecordCount}", admins!.TelegramBotAdmins.Count);
        foreach (var admin in admins.TelegramBotAdmins)
        {
            await botClient.SendMessage(admin.TelegramId, message, cancellationToken: cancellationToken);
        }
    }

    public async Task NotifyAdminsAboutExceptionAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var admins = await telegramUsersService.GetAdminsAsync(cancellationToken);

        if (admins.TelegramBotAdmins is { Count: 0 })
        {
            logger.LogWarning("No admins are configured to receive error notifications.");
            return;
        }

        logger.LogInformation($"Notifying {admins!.TelegramBotAdmins.Count} admins about an error.");

        foreach (var admin in admins.TelegramBotAdmins)
        {
            try
            {
                var source = "Unknown";
                if (context is { Request: not null })
                {
                    source = context?.Request?.Path.Value ?? "Unknown";
                }
                
                var stackTrace = exception.StackTrace ?? "No stack trace available.";
                if (stackTrace.Length > 3000)
                {
                    stackTrace = stackTrace.Substring(0, 3000) + "... (truncated)";
                }
                
                var errorMessage = $"🚨 *Error Notification*\n" +
                                   $"Path: `{source}`\n" +
                                   $"Message: `{exception.Message}`\n" +
                                   $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                   $"Stack Trace:\n```{stackTrace}```";

                if (errorMessage.Length > 4096)
                {
                    errorMessage = errorMessage.Substring(0, 4093) + "...";
                }
                
                await botClient.SendMessage(admin.TelegramId, errorMessage,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send error notification to admin with Telegram ID {admin.TelegramId}.");
            }
        }
    }
    
    public async Task NotifyAdminsAboutStartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var telegramUsersService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var admins = await telegramUsersService.GetAdminsAsync(cancellationToken);
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        if (admins.TelegramBotAdmins is { Count: 0 })
        {
            logger.LogWarning("Admin chat ID is not configured.");
            return;
        }
        logger.LogInformation("Admins count: {RecordCount}", admins!.TelegramBotAdmins.Count);
        foreach (var admin in admins.TelegramBotAdmins)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";

            var startupMessage = $"🚀 Bot started successfully!\n" +
                                 $"Application version: {version}\n" +
                                 $"Environment: {environment.EnvironmentName}\n" +
                                 $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            await botClient.SendMessage(admin.TelegramId, startupMessage, cancellationToken: cancellationToken);
        }
    }
}