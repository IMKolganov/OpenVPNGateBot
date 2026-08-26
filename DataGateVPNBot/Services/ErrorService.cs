using System.Reflection;
using DataGateVPNBot.Models;
using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services;

public class ErrorService(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IAdminRecipientService adminRecipientService,
    ILogger<ErrorService> logger)
    : IErrorService
{
    private static bool _isSendingException;

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
        var adminIds = await adminRecipientService.GetAdminTelegramIdsAsync(cancellationToken, tryRefresh: false);
        if (adminIds.Count == 0)
        {
            logger.LogWarning("Admin chat ID is not configured.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        logger.LogInformation("Sending admin message to {RecordCount} recipient(s).", adminIds.Count);
        foreach (var adminId in adminIds)
        {
            try
            {
                await botClient.SendMessage(adminId, message, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send admin message to Telegram ID {TelegramId}.",
                    adminId);
            }
        }
    }

    public async Task SendPhotoToAdminsAsync(
        byte[]? photoBytes,
        string caption,
        string fileName = "avatar.jpg",
        CancellationToken cancellationToken = default)
    {
        if (photoBytes is not { Length: > 0 })
        {
            await SendMessageToAdminsAsync(caption, cancellationToken);
            return;
        }

        var adminIds = await adminRecipientService.GetAdminTelegramIdsAsync(cancellationToken, tryRefresh: false);
        if (adminIds.Count == 0)
        {
            logger.LogWarning("Admin chat ID is not configured.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        logger.LogInformation("Sending photo alert to {RecordCount} recipient(s).", adminIds.Count);
        foreach (var adminId in adminIds)
        {
            try
            {
                await using var stream = new MemoryStream(photoBytes, writable: false);
                await botClient.SendPhoto(
                    adminId,
                    new InputFileStream(stream, fileName),
                    caption: caption,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send admin photo to Telegram ID {TelegramId}; falling back to text.",
                    adminId);
                try
                {
                    await botClient.SendMessage(adminId, caption, cancellationToken: cancellationToken);
                }
                catch (Exception textEx)
                {
                    logger.LogError(textEx,
                        "Failed to send fallback admin text to Telegram ID {TelegramId}.",
                        adminId);
                }
            }
        }
    }


    public async Task NotifyAdminsAboutExceptionAsync(Exception exception, HttpContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (_isSendingException)
        {
            logger.LogWarning("Skipping recursive call to NotifyAdminsAboutExceptionAsync.");
            return;
        }

        _isSendingException = true;

        try
        {
            var adminIds = await adminRecipientService.GetAdminTelegramIdsAsync(cancellationToken, tryRefresh: false);
            if (adminIds.Count == 0)
            {
                logger.LogWarning("No admins are configured to receive error notifications.");
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            logger.LogInformation("Notifying {Count} admin(s) about an error.", adminIds.Count);

            foreach (var adminId in adminIds)
            {
                try
                {
                    var source = context?.Request?.Path.Value ?? "Unknown";
                    var stackTrace = exception.StackTrace ?? "No stack trace available.";

                    if (stackTrace.Length > 3000)
                        stackTrace = stackTrace[..3000] + "... (truncated)";

                    var errorMessage = $"🚨 *Error Notification*\n" +
                                       $"Path: `{source}`\n" +
                                       $"Message: `{exception.Message}`\n" +
                                       $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                       $"Stack Trace:\n```{stackTrace}```";

                    if (errorMessage.Length > 4096)
                        errorMessage = errorMessage[..4093] + "...";

                    await botClient.SendMessage(adminId, errorMessage,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to send error notification to admin with Telegram ID {TelegramId}.",
                        adminId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed inside NotifyAdminsAboutExceptionAsync itself.");
        }
        finally
        {
            _isSendingException = false;
        }
    }

    public async Task NotifyAdminsAboutStartAsync(CancellationToken cancellationToken)
    {
        var adminIds = await adminRecipientService.GetAdminTelegramIdsAsync(cancellationToken, tryRefresh: false);
        if (adminIds.Count == 0)
        {
            logger.LogWarning("Admin chat ID is not configured.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        logger.LogInformation("Notifying {RecordCount} admin(s) about startup.", adminIds.Count);
        foreach (var adminId in adminIds)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";

            var startupMessage = $"🚀 Bot started successfully!\n" +
                                 $"Application version: {version}\n" +
                                 $"Environment: {environment.EnvironmentName}\n" +
                                 $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            await botClient.SendMessage(adminId, startupMessage, cancellationToken: cancellationToken);
        }
    }
}
