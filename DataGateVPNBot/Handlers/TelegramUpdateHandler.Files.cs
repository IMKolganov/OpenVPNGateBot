using DataGateVPNBot.Models.DashBoardApi;
using DataGateVPNBot.Services.DashboardServices;
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
        var dashBoardApiOvpnFileService = scope.ServiceProvider.GetRequiredService<DashBoardApiOvpnFileService>();
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

        var issuedOvpnFileResponses = await dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
            vpnServerId, msg.From!.Id.ToString(), cancellationToken);

        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<IssuedOvpnFileResponse>();

        if (issuedOvpnFileResponses is not { Count: > 0 })
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("FilesNotFoundError", msg.From!.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Multiple configuration files detected. Preparing media group...");
        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();

        try
        {
            foreach (var issuedOvpnFileResponse in issuedOvpnFileResponses)
            {
                var issuedOvpnFileStream = await dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    issuedOvpnFileResponse.Id, issuedOvpnFileResponse.ServerId, cancellationToken);

                _logger.LogInformation("Processing file: {FileName}", issuedOvpnFileResponse.FileName);

                var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFileResponse.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = issuedOvpnFileResponse.FileName
                };
                mediaGroupOpenVpnFiles.Add(media);
            }

            if (mediaGroupOpenVpnFiles.Count == 0)
            {
                return await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: await GetLocalizationTextAsync("FilesDownloadError", msg.From!.Id, cancellationToken),
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending media group.");
            throw;
        }
    }

    private async Task<Message> MakeNewVpnFile(Message msg, CancellationToken cancellationToken)
    {
        // if (check )//todo: need check
        //throw
        
        // Generate the client configuration file
        if (!_openVpnClientService.CheckHealthFileSystem()) 
            return await InformationClientAboutCertCriticalError(msg, cancellationToken);
        var clientConfigFile = await _openVpnClientService.CreateClientConfiguration(msg.Chat.Id, cancellationToken);
        if (clientConfigFile.FileInfo != null)
        {
            _logger.LogInformation("Client configuration created successfully in UpdateHandler.");
            await _botClient.SendChatAction(msg.Chat.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);
            // Send the .ovpn file to the user
            await using var fileStream = new FileStream(clientConfigFile.FileInfo.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read);
            return await _botClient.SendDocument(
                chatId: msg.Chat.Id,
                document: InputFile.FromStream(fileStream, clientConfigFile.FileInfo.Name),
                caption: clientConfigFile.Message, 
                cancellationToken: cancellationToken);
        }
        else
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: clientConfigFile.Message,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }
    }
    
    private async Task<Message> DeleteAllFiles(Message msg, CancellationToken cancellationToken)
    {
        if (!_openVpnClientService.CheckHealthFileSystem()) 
            return await InformationClientAboutCertCriticalError(msg, cancellationToken);
        await _openVpnClientService.DeleteAllClientConfigurations(msg.From!.Id);
        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: await GetLocalizationTextAsync("SuccessfullyDeletedAllFile", msg.From!.Id, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DeleteSelectedFile(Message msg, CancellationToken cancellationToken)
    {
        if (!_openVpnClientService.CheckHealthFileSystem()) 
            return await InformationClientAboutCertCriticalError(msg, cancellationToken);
        var clientConfigFiles = await _openVpnClientService.GetAllClientConfigurations(msg.From!.Id, cancellationToken);
        var rows = new List<InlineKeyboardButton[]>();

        var currentRow = new List<InlineKeyboardButton>();
        foreach (var fileInfo in clientConfigFiles.FileInfo)
        {
            currentRow.Add(InlineKeyboardButton.WithCallbackData(fileInfo.Name, $"/delete_file {fileInfo.Name}"));

            if (currentRow.Count == 2)
            {
                rows.Add(currentRow.ToArray());
                currentRow.Clear();
            }
        }
        
        if (currentRow.Count > 0)
        {
            rows.Add(currentRow.ToArray());
        }

        var inlineMarkup = new InlineKeyboardMarkup(rows);
        return await _botClient.SendMessage(
            msg.Chat,
            await GetLocalizationTextAsync("ChooseFileForDelete", msg.From!.Id, cancellationToken),
            replyMarkup: inlineMarkup, 
            cancellationToken: cancellationToken);
    }

    private async Task DeleteFile(long telegramId, string fileName, CancellationToken cancellationToken)
    {
        if (!_openVpnClientService.CheckHealthFileSystem()) throw new Exception("Unable to delete file");
        await _openVpnClientService.DeleteClientConfiguration(telegramId, fileName);
        await _botClient.SendMessage(
            chatId: telegramId,
            text: await GetLocalizationTextAsync("SuccessfullyDeletedFile", telegramId, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
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