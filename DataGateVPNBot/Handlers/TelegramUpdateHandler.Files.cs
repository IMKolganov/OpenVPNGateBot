using DataGateVPNBot.Services.BotServices.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    
    private async Task<Message> DashBoardApiGetToken(Message msg)
    {
        string? token = await _dashBoardApiAuthService.GetTokenAsync();
        
        if (token == null)
        {
            return await _botClient.SendMessage(msg.Chat, 
                $"Authentication failed. Please try again. Token: {token}",
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove());
        }
        
        return await _botClient.SendMessage(msg.Chat, 
            $"Authentication successful! Token received. Token: {token}",
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> GetMyFiles(Message msg, string vpnServerIdArg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("InvalidServerId", msg.From!.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
        _logger.LogInformation($"GetMyFiles started for user: {msg.From?.Id}, ServerId: {vpnServerId}");

        var mediaGroupOpenVpnFiles = await ovpnFileService.GetOvpnFilesAsync(vpnServerId, msg.From!.Id, cancellationToken);

        if (!mediaGroupOpenVpnFiles.Any())
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("FilesNotFoundError", msg.From!.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Sending media group...");
        var messages = await _botClient.SendMediaGroup(
            chatId: msg.Chat.Id,
            media: mediaGroupOpenVpnFiles,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Media group sent successfully.");

        return messages.FirstOrDefault() ?? throw new InvalidOperationException("No messages returned after sending media group.");
    }

    private async Task<Message> MakeNewVpnFile(Message msg, string vpnServerIdArg, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);
        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();
        
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("InvalidServerId", msg.From!.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        
        var newOvpnFile = await ovpnFileService.MakeOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken);
        if (newOvpnFile != null)
        {
            _logger.LogInformation("Client configuration created successfully in UpdateHandler.");

            return await _botClient.SendDocument(
                chatId: msg.Chat.Id,
                document: newOvpnFile,
                caption: "1234", 
                cancellationToken: cancellationToken);
        }
        else
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: "1423",
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
    }
    
    private async Task<Message> DeleteAllFiles(Message msg, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // if (!_openVpnClientService.CheckHealthFileSystem()) 
        //     return await InformationClientAboutCertCriticalError(msg, cancellationToken);
        // await _openVpnClientService.DeleteAllClientConfigurations(msg.From!.Id);
        // return await _botClient.SendMessage(
        //     chatId: msg.Chat.Id,
        //     text: await GetLocalizationTextAsync("SuccessfullyDeletedAllFile", msg.From!.Id, cancellationToken),
        //     replyMarkup: new ReplyKeyboardRemove(), 
        //     cancellationToken: cancellationToken);
    }

    private async Task<Message> DeleteSelectedFile(Message msg, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // if (!_openVpnClientService.CheckHealthFileSystem()) 
        //     return await InformationClientAboutCertCriticalError(msg, cancellationToken);
        // var clientConfigFiles = await _openVpnClientService.GetAllClientConfigurations(msg.From!.Id, cancellationToken);
        // var rows = new List<InlineKeyboardButton[]>();
        //
        // var currentRow = new List<InlineKeyboardButton>();
        // foreach (var fileInfo in clientConfigFiles.FileInfo)
        // {
        //     currentRow.Add(InlineKeyboardButton.WithCallbackData(fileInfo.Name, $"/delete_file {fileInfo.Name}"));
        //
        //     if (currentRow.Count == 2)
        //     {
        //         rows.Add(currentRow.ToArray());
        //         currentRow.Clear();
        //     }
        // }
        //
        // if (currentRow.Count > 0)
        // {
        //     rows.Add(currentRow.ToArray());
        // }
        //
        // var inlineMarkup = new InlineKeyboardMarkup(rows);
        // return await _botClient.SendMessage(
        //     msg.Chat,
        //     await GetLocalizationTextAsync("ChooseFileForDelete", msg.From!.Id, cancellationToken),
        //     replyMarkup: inlineMarkup, 
        //     cancellationToken: cancellationToken);
    }

    private async Task DeleteFile(long telegramId, string fileName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // if (!_openVpnClientService.CheckHealthFileSystem()) throw new Exception("Unable to delete file");
        // await _openVpnClientService.DeleteClientConfiguration(telegramId, fileName);
        // await _botClient.SendMessage(
        //     chatId: telegramId,
        //     text: await GetLocalizationTextAsync("SuccessfullyDeletedFile", telegramId, cancellationToken),
        //     replyMarkup: new ReplyKeyboardRemove(), 
        //     cancellationToken: cancellationToken);
    }

    private async Task<Message>  InformationClientAboutCertCriticalError(Message msg, 
        CancellationToken cancellationToken)
    {
        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: await GetLocalizationTextAsync("CertCriticalError", msg.From!.Id, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }
}