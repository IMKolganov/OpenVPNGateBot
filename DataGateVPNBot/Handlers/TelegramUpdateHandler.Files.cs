using DataGateVPNBot.Helpers;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private static bool ServerIsXray(DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto.VpnServerDto? server) =>
        server is { ServerType: VpnServerType.Xray };

    private async Task<bool> IsXrayServerAsync(IServiceScope scope, int vpnServerId, CancellationToken cancellationToken)
    {
        var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
        var server = await serverService.GetVpnServerByIdAsync(vpnServerId, cancellationToken);
        return ServerIsXray(server);
    }

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
        foreach (var server in serverResponses.VpnServers)
        {
            if (VpnServerDtoReflection.IsDisabled(server))
                continue;

            var label = server.ServerType == VpnServerType.Xray
                ? $"{server.ServerName} (VLESS)"
                : server.ServerName;
            currentRow.Add(InlineKeyboardButton.WithCallbackData(label,
                $"{command} {server.Id}"));
        
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
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, BotCommands.CommandGetMyFiles, cancellationToken);
        }

        _logger.LogInformation($"GetMyFiles started for user: {msg.Chat.Id}, ServerId: {vpnServerId}");

        var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
        var mediaGroupOpenVpnFiles = isXray
            ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .GetOvpnFilesAsync(vpnServerId, msg.Chat.Id, cancellationToken)
            : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                .GetOvpnFilesAsync(vpnServerId, msg.Chat.Id, cancellationToken);

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
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, BotCommands.CommandGetMyFilesWithToken, cancellationToken);
        }

        _logger.LogInformation($"GetMyFiles started for user: {msg.Chat.Id}, ServerId: {vpnServerId}");

        var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
        if (isXray)
        {
            var items = await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .GetClientLinkItemsWithTokenAsync(vpnServerId, msg.Chat.Id, cancellationToken);

            if (items.Count == 0)
            {
                return await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            Message? first = null;
            foreach (var item in items)
            {
                var copyText = TelegramCopyTextHelper.TryGetVlessCopyText(item.Text);
                InlineKeyboardMarkup? markup = null;
                if (!string.IsNullOrWhiteSpace(copyText))
                {
                    markup = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCopyText(
                            "📋 Copy VLESS",
                            new CopyTextButton { Text = copyText }));
                }
                var sent = await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: item.Text,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken);
                first ??= sent;
            }

            return first ?? throw new InvalidOperationException("No messages returned after sending XRay links.");
        }

        var mediaGroupOpenVpnFiles = isXray
            ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .GetOvpnFilesWithTokenAsync(vpnServerId, msg.Chat.Id, _botConfig.HostAddress, cancellationToken)
            : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                .GetOvpnFilesWithTokenAsync(vpnServerId, msg.Chat.Id, _botConfig.HostAddress, cancellationToken);

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
            if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
            {
                return await GetOpenVpnServers(msg, BotCommands.CommandMakeNewFile, cancellationToken);
            }

            var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
            var atLimit = isXray
                ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                    .CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken)
                : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                    .CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken);
            if (atLimit)
            {
                return await _botClient.SendMessage(
                    msg.Chat,
                    await GetLocalizationTextAsync("MaxConfigError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            var mediaGroupOpenVpnFiles = isXray
                ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                    .MakeOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken)
                : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                    .MakeOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken);
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
            if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
            {
                return await GetOpenVpnServers(msg, BotCommands.CommandMakeNewFileWithToken, cancellationToken);
            }

            var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
            var atLimit = isXray
                ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                    .CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken)
                : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                    .CheckMaxCountOvpnFilesForClient(vpnServerId, msg.Chat.Id, cancellationToken);
            if (atLimit)
            {
                return await _botClient.SendMessage(
                    msg.Chat,
                    await GetLocalizationTextAsync("MaxConfigError", msg.Chat.Id, cancellationToken),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            var mediaGroupOpenVpnFiles = isXray
                ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                    .MakeOvpnFileWithTokenAsync(vpnServerId, msg.Chat.Id, _botConfig.HostAddress,
                        cancellationToken)
                : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                    .MakeOvpnFileWithTokenAsync(vpnServerId, msg.Chat.Id, _botConfig.HostAddress,
                        cancellationToken);

            if (isXray)
            {
                var item = await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                    .MakeClientLinkItemWithTokenAsync(vpnServerId, msg.Chat.Id, cancellationToken);

                if (item is null || string.IsNullOrWhiteSpace(item.Value.Text))
                {
                    return await _botClient.SendMessage(
                        chatId: msg.Chat.Id,
                        text: await GetLocalizationTextAsync("FilesNotFoundError", msg.Chat.Id, cancellationToken),
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }

                var copyText = TelegramCopyTextHelper.TryGetVlessCopyText(item.Value.Text);
                InlineKeyboardMarkup? markup = null;
                if (!string.IsNullOrWhiteSpace(copyText))
                {
                    markup = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCopyText(
                            "📋 Copy VLESS",
                            new CopyTextButton { Text = copyText }));
                }
                return await _botClient.SendMessage(
                    chatId: msg.Chat.Id,
                    text: item.Value.Text,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken);
            }

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
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, "/delete_all_files", cancellationToken);
        }

        var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
        var revoked = isXray
            ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .RevokeAllOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken)
            : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                .RevokeAllOvpnFileAsync(vpnServerId, msg.Chat.Id, cancellationToken);
        if (revoked)
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
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            return await GetOpenVpnServers(msg, BotCommands.CommandDeleteSelectedFile, cancellationToken);
        }

        var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
        var clientConfigFiles = isXray
            ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .GetAllOvpnFilesListAsync(vpnServerId, msg.Chat.Id, cancellationToken)
            : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                .GetAllOvpnFilesListAsync(vpnServerId, msg.Chat.Id, cancellationToken);

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
        if (!int.TryParse(vpnServerIdArg, out int vpnServerId))
        {
            await _botClient.SendMessage(
                chatId: telegramId,
                text: await GetLocalizationTextAsync("InvalidServerId", telegramId, cancellationToken),
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            return;
        }

        var isXray = await IsXrayServerAsync(scope, vpnServerId, cancellationToken);
        var ok = isXray
            ? await scope.ServiceProvider.GetRequiredService<IXrayClientLinkBotService>()
                .RevokeOvpnFileAsync(vpnServerId, telegramId, fileName, cancellationToken)
            : await scope.ServiceProvider.GetRequiredService<IOvpnFileService>()
                .RevokeOvpnFileAsync(vpnServerId, telegramId, fileName, cancellationToken);
        if (ok)
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