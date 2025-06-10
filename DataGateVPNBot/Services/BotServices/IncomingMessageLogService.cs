using System.Text.Json;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class IncomingMessageLogService(IIncomingMessageLogSenderService incomingMessageLogSenderService) 
    : IIncomingMessageLogService
{
    public async Task Log(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
    {
        var request = new AddMessageRequest
        {
            Message = new MessageDto
            {
                TelegramId = msg.From?.Id ?? 0,
                Username = msg.From?.Username,
                FirstName = msg.From?.FirstName,
                LastName = msg.From?.LastName,
                MessageText = msg.Text ?? string.Empty,
                ReceivedAt = DateTime.UtcNow
            }
        };

        try
        {
            if (msg.Document != null)
            {
                await ProcessFileAsync(botClient, msg.Document.FileId, msg.Document.FileName,
                    msg.Document.FileSize, "Document", request.Message, cancellationToken);
            }
            else if (msg.Photo?.Any() == true)
            {
                var largestPhoto = msg.Photo.OrderByDescending(p => p.FileSize).First();
                var photoFileName = $"photo_{request.Message.TelegramId}_{Guid.NewGuid():N}.jpg";

                await ProcessFileAsync(botClient, largestPhoto.FileId, photoFileName, largestPhoto.FileSize,
                    "Photo", request.Message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            request.Message.MessageText += $"\n[Error processing file: {ex.Message}]";
        }

        await incomingMessageLogSenderService.TelegramBotIncomingMessageLogAddMessageAsync(request, cancellationToken);

        await SaveToFileAsync(request, cancellationToken);
    }

    private async Task ProcessFileAsync(
        ITelegramBotClient botClient,
        string fileId,
        string? fileName,
        long? fileSize,
        string fileType,
        MessageDto log,
        CancellationToken cancellationToken)
    {
        if (fileSize > 10 * 1024 * 1024)
            throw new Exception("File size exceeds the 10MB limit.");

        var filePath = Path.Combine("SavedFiles", fileName ?? "file");
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await botClient.GetInfoAndDownloadFile(fileId, fileStream, cancellationToken);

        log.FileType = fileType;
        log.FileId = fileId;
        log.FileName = fileName;
        log.FileSize = fileSize;
        log.FilePath = filePath;
    }

    private async Task SaveToFileAsync(AddMessageRequest request, CancellationToken cancellationToken)
    {
        var dir = Path.Combine("Logs");
        Directory.CreateDirectory(dir);

        var fileName = $"{request.Message!.TelegramId}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.json";
        var fullPath = Path.Combine(dir, fileName);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(fullPath, json, cancellationToken);
    }
}