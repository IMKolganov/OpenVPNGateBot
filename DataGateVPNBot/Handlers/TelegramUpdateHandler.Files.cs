using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    
    private async Task<Message> DashBoardApiGetToken(Message msg)
    {
        string? token = await authService.GetTokenAsync();
        
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

    private async Task<Message> GetOpenVpnServers(Message msg, string command, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var openVpnServersService = scope.ServiceProvider.GetRequiredService<IOpenVpnServersService>();
        var serverResponses = await openVpnServersService.
            GetAllOpenVpnServersListAsync(cancellationToken);
        
        var rows = new List<InlineKeyboardButton[]>();
        var currentRow = new List<InlineKeyboardButton>();
        foreach (var server in serverResponses)
        {
            currentRow.Add(InlineKeyboardButton.WithCallbackData(server.OpenVpnServer.ServerName, 
                $"{command} {server.OpenVpnServer.Id}"));
        
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
            msg.From!.Id,
            await GetLocalizationTextAsync("ChooseOpenVpnServer", msg.Chat.Id, cancellationToken),
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
            return await GetOpenVpnServers(msg, BotCommands.CommandGetMyFiles, cancellationToken);
        }

        _logger.LogInformation($"GetMyFiles started for user: {msg.Chat.Id}, ServerId: {vpnServerId}");

        var mediaGroupOpenVpnFiles = await ovpnFileService.GetOvpnFilesAsync(vpnServerId,
            msg.Chat.Id, cancellationToken);

        if (!mediaGroupOpenVpnFiles.Any())
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Sending media group...");
        var messages = await _botClient.SendMediaGroup(
            chatId: msg.Chat.Id,
            media: mediaGroupOpenVpnFiles,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Media group sent successfully.");

        return messages.FirstOrDefault() ??
               throw new InvalidOperationException("No messages returned after sending media group.");
    }
    
    private async Task<Message> GetMyFilesWithToken(Message msg, string? vpnServerIdArg, 
        CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, BotCommands.CommandGetMyFilesWithToken, cancellationToken);
        }

        _logger.LogInformation($"GetMyFiles started for user: {msg.Chat.Id}, ServerId: {vpnServerId}");

        var mediaGroupOpenVpnFiles = await ovpnFileService.GetOvpnFilesWithTokenAsync(vpnServerId,
            msg.Chat.Id, _botConfig.HostAddress, cancellationToken);

        if (!mediaGroupOpenVpnFiles.Any())
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Sending media group...");
        var messages = await _botClient.SendMediaGroup(
            chatId: msg.Chat.Id,
            media: mediaGroupOpenVpnFiles,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Media group sent successfully.");

        return messages.FirstOrDefault() ??
               throw new InvalidOperationException("No messages returned after sending media group.");
    }

    private async Task<Message> MakeNewVpnFile(Message msg, string? vpnServerIdArg, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

            if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
            {
                return await GetOpenVpnServers(msg, BotCommands.CommandMakeNewFile, cancellationToken);
            }

            if (await ovpnFileService.CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken))
            {
                return await _botClient.SendMessage(
                    msg.Chat,
                    await GetLocalizationTextAsync("MaxConfigError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            var mediaGroupOpenVpnFiles =
                await ovpnFileService.MakeOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken);
            if (!mediaGroupOpenVpnFiles.Any())
            {
                return await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Sending media group...");
            var messages = await _botClient.SendMediaGroup(
                chatId: msg.Chat.Id,
                media: mediaGroupOpenVpnFiles,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Media group sent successfully.");

            return messages.FirstOrDefault() ??
                   throw new InvalidOperationException("No messages returned after sending media group.");
        }
        catch(Exception ex)
        {
            using var scope = serviceProvider.CreateScope();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            return await _botClient.SendMessage(
                msg.Chat,
                await GetLocalizationTextAsync("SomethingWentWrongWhenTryMakeNewFile", 
                    msg.Chat.Id, cancellationToken) + " Details: " + ex.Message,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
    }
    
    private async Task<Message> MakeNewVpnFileWithToken(Message msg, string? vpnServerIdArg, 
            CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.From!.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

            if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
            {
                return await GetOpenVpnServers(msg, BotCommands.CommandMakeNewFileWithToken, cancellationToken);
            }

            if (await ovpnFileService.CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken))
            {
                return await _botClient.SendMessage(
                    msg.Chat,
                    await GetLocalizationTextAsync("MaxConfigError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            var mediaGroupOpenVpnFiles =
                await ovpnFileService.MakeOvpnFileWithTokenAsync(vpnServerId, msg.Chat.Id, _botConfig.HostAddress, 
                    cancellationToken);
            if (!mediaGroupOpenVpnFiles.Any())
            {
                return await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Sending media group...");
            var messages = await _botClient.SendMediaGroup(
                chatId: msg.Chat.Id,
                media: mediaGroupOpenVpnFiles,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Media group sent successfully.");

            return messages.FirstOrDefault() ??
                   throw new InvalidOperationException("No messages returned after sending media group.");
        }
        catch(Exception ex)
        {
            using var scope = serviceProvider.CreateScope();
            var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            return await _botClient.SendMessage(
                msg.Chat,
                await GetLocalizationTextAsync("SomethingWentWrongWhenTryMakeNewFile", 
                    msg.Chat.Id, cancellationToken) + " Details: " + ex.Message,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
    }
    
    private async Task<Message> DeleteAllFiles(Message msg, string? vpnServerIdArg, CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, "/delete_all_files", cancellationToken);
        }

        if (await ovpnFileService.RevokeAllOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken))
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("SuccessfullyDeletedAllFile", msg.Chat.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(), 
                cancellationToken: cancellationToken);
        }

        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: await GetLocalizationTextAsync("ErrorDeletedAllFile", msg.Chat.Id, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> DeleteSelectedFile(Message msg, string? vpnServerIdArg, 
        CancellationToken cancellationToken)
    {
        await _botClient.SendChatAction(msg.Chat.Id, ChatAction.UploadDocument, cancellationToken: cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var ovpnFileService = scope.ServiceProvider.GetRequiredService<IOvpnFileService>();

        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, BotCommands.CommandDeleteSelectedFile, cancellationToken);
        }
        
        var clientConfigFiles = await ovpnFileService.GetAllOvpnFilesListAsync(vpnServerId,
            msg.Chat.Id, cancellationToken);

        if (clientConfigFiles.Count <= 0)
        {
            return await _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: await GetLocalizationTextAsync("ErrorDeletedAllFile", msg.Chat.Id, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(), 
                cancellationToken: cancellationToken);
        }
        
        var rows = new List<InlineKeyboardButton[]>();
        
        var currentRow = new List<InlineKeyboardButton>();
        foreach (var fileInfo in clientConfigFiles)
        {
            currentRow.Add(InlineKeyboardButton.WithCallbackData(fileInfo.FileName, 
                $"{BotCommands.CommandDeleteSelectedFile} {vpnServerId} {fileInfo.FileName}"));
        
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
            await GetLocalizationTextAsync("ChooseFileForDelete", msg.Chat.Id, cancellationToken),
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
        else
        {
            await _botClient.SendMessage(
                chatId: telegramId,
                text: await GetLocalizationTextAsync("ErrorDeletedFile",telegramId, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(), 
                cancellationToken: cancellationToken);
        }
    }
}