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

    private async Task<Message> GetOpenVpnServers(Message msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var openVpnServersService = scope.ServiceProvider.GetRequiredService<IOpenVpnServersService>();
        var serverResponses = await openVpnServersService.
            GetAllOpenVpnServersListAsync(cancellationToken);
        
        var rows = new List<InlineKeyboardButton[]>();
        var currentRow = new List<InlineKeyboardButton>();
        foreach (var server in serverResponses)
        {
            currentRow.Add(InlineKeyboardButton.WithCallbackData(server.ServerName, 
                $"/get_my_files {server.Id}"));
        
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
            await GetLocalizationTextAsync("ChooseOpenVpnServer", msg.From!.Id, cancellationToken),
            replyMarkup: inlineMarkup, 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> GetMyFiles(Message msg, string? vpnServerIdArg, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, cancellationToken);
        }
        
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
    
    private async Task<Message> DeleteAllFiles(Message msg, string vpnServerIdArg, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
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

        if (await ovpnFileService.RevokeAllOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken))
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("SuccessfullyDeletedAllFile", msg.From!.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(), 
                cancellationToken: cancellationToken);
        }

        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: await GetLocalizationTextAsync("ErrorDeletedAllFile", msg.From!.Id, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DeleteSelectedFile(Message msg, string vpnServerIdArg, 
        CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
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
        
        var clientConfigFiles = await ovpnFileService.GetAllOvpnFilesListAsync(vpnServerId,
            msg.From!.Id, cancellationToken);
        
        var rows = new List<InlineKeyboardButton[]>();
        
        var currentRow = new List<InlineKeyboardButton>();
        foreach (var fileInfo in clientConfigFiles)
        {
            currentRow.Add(InlineKeyboardButton.WithCallbackData(fileInfo.FileName, $"/delete_file {fileInfo.FileName}"));
        
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

    private async Task DeleteFile(long telegramId, string vpnServerIdArg, string fileName, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(telegramId, ChatAction.Typing, cancellationToken: cancellationToken);
        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();
        
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            await _botClient.SendMessage(
                chatId: telegramId,
                text: await GetLocalizationTextAsync("InvalidServerId", telegramId, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        if (await ovpnFileService.RevokeOvpnFileAsync(vpnServerId, telegramId, fileName, cancellationToken))
        {
            await _botClient.SendMessage(
                chatId: telegramId,
                text: await GetLocalizationTextAsync("SuccessfullyDeletedFile", telegramId, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(), 
                cancellationToken: cancellationToken);
        }

        await _botClient.SendMessage(
            chatId: telegramId,
            text: await GetLocalizationTextAsync("ErrorDeletedFile",telegramId, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }
}