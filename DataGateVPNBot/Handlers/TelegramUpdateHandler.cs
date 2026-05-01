using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler(
    ILogger<TelegramUpdateHandler> logger,
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider,
    ITelegramSettingsService telegramSettingsService,
    AuthService authService,
    IOptions<BotConfiguration> options)
    : IUpdateHandler
{
    private readonly ILogger<TelegramUpdateHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ITelegramBotClient _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? 
                                                         throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ITelegramSettingsService _telegramSettingsService = 
        telegramSettingsService ?? throw new ArgumentNullException(nameof(telegramSettingsService));
    private readonly BotConfiguration _botConfig = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    #region HandleErrorAsync: Error handling for Telegram Bot API
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogCritical("HandleError: {Exception}", exception);
        using var scope = _serviceProvider.CreateScope();

        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        errorService.LogErrorToDatabase(exception);//todo:fix it
        await errorService.NotifyAdminsAboutExceptionAsync(exception, null, cancellationToken);
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    #endregion
    
    #region  Handles incoming updates from Telegram Bot API and routes them to specific handlers.
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message } => OnMessage(message, cancellationToken),
            { EditedMessage: { } message } => OnMessage(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery, cancellationToken),
            { ChannelPost: { } channelPost } => OnUnsupportedChannelUpdateAsync("ChannelPost", channelPost, cancellationToken),
            { EditedChannelPost: { } editedChannelPost } => OnUnsupportedChannelUpdateAsync("EditedChannelPost", editedChannelPost, cancellationToken),
            { MyChatMember: { } myChatMember } => OnMyChatMemberUpdate(myChatMember, cancellationToken),
            { ChatMember: { } chatMember } => OnChatMemberUpdate(chatMember, cancellationToken),
            { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll } => OnPoll(poll),
            { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
            // ChannelPost:
            // EditedChannelPost:
            // ShippingQuery:
            // PreCheckoutQuery:
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        });
    }
    #endregion
    
    #region OnMessage: Handle incoming messages
    private async Task OnMessage(Message msg, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
            return;
        await LogIncomingMessage(msg, cancellationToken);

        Message sentMessage = await ProcessingMessage(msg, messageText, cancellationToken);
        _logger.LogInformation("Message sent with id: {SentMessageId}", sentMessage.Id);
    }
    #endregion

    private async Task<Message> ProcessingMessage(Message msg, string messageText, CancellationToken cancellationToken)
    {
        var commandParts = messageText.Split(' ', 2);
        var rawCommand = commandParts[0].ToLower();
        var command = rawCommand.Split('@')[0]; // remove @BotUsername
        var argument = commandParts.Length > 1 ? commandParts[1] : null;

        var isPrivate = msg.Chat.Type == ChatType.Private;

        await RegisterNewUserAsync(msg, cancellationToken); // optional user registration

        var isLocalizationCommand = command is BotCommands.CommandStart
            or BotCommands.CommandChangeLanguage
            or BotCommands.CommandEnglish
            or BotCommands.CommandRussian
            or BotCommands.CommandGreek;

        if (!await IsExistLocalizationSettings(msg.From!.Id, cancellationToken) && !isLocalizationCommand)
        {
            _logger.LogInformation(
                "Localization settings not found for TelegramId: {TelegramId}. Triggering language selection.",
                msg.From.Id);
            return await SelectLanguage(msg, cancellationToken);
        }

        _logger.LogInformation("Processing command {Command} from user {UserId}", command, msg.From!.Id);

        var privateOnlyCommands = new HashSet<string>
        {
            BotCommands.CommandRegister,
            BotCommands.CommandGetMyFiles,
            BotCommands.CommandMakeNewFile,
            BotCommands.CommandMakeNewFileWithToken,
            BotCommands.CommandDeleteSelectedFile,
            BotCommands.CommandDeleteAllFiles,
            BotCommands.CommandDashboardApiGetToken,
            BotCommands.CommandRefreshProfilePhotos
        };

        if (!isPrivate && privateOnlyCommands.Contains(command))
        {
            _logger.LogInformation("Command {Command} is not allowed in group chats.", command);
            return await  _botClient.SendMessage(
                chatId: msg.Chat.Id,
                text: $"Command {command} is not allowed in group chats.", 
                cancellationToken: cancellationToken);
        }

        return await (command switch
        {
            BotCommands.CommandStart => Start(msg, cancellationToken),
            BotCommands.CommandAboutBot => AboutBot(msg, cancellationToken),
            BotCommands.CommandHowToUse => HowToUseVpn(msg, cancellationToken),
            BotCommands.CommandRegister => RegisterForVpn(msg, cancellationToken),
            BotCommands.CommandGetMyFiles => GetMyFilesWithToken(msg, argument, cancellationToken),
            BotCommands.CommandGetMyFilesWithToken => GetMyFilesWithToken(msg, argument, cancellationToken),
            BotCommands.CommandGetMyFilesWithoutToken => GetMyFiles(msg, argument, cancellationToken),
            BotCommands.CommandMakeNewFile => MakeNewVpnFileWithToken(msg, argument, cancellationToken),
            BotCommands.CommandMakeNewFileWithoutToken => MakeNewVpnFile(msg, argument, cancellationToken),
            BotCommands.CommandMakeNewFileWithToken => MakeNewVpnFileWithToken(msg, argument, cancellationToken),
            BotCommands.CommandDeleteSelectedFile => DeleteSelectedFile(msg, argument, cancellationToken),
            BotCommands.CommandDeleteAllFiles => DeleteAllFiles(msg, argument, cancellationToken),
            BotCommands.CommandInstallClient => InstallClient(msg, cancellationToken),
            BotCommands.CommandAboutProject => AboutProject(msg, cancellationToken),
            BotCommands.CommandContacts => Contacts(msg, cancellationToken),
            BotCommands.CommandChangeLanguage => SelectLanguage(msg, cancellationToken),
            BotCommands.CommandRegisterCommands => RegisterCommandsAsync(msg, cancellationToken),
            BotCommands.CommandEnglish or BotCommands.CommandRussian or BotCommands.CommandGreek => ChangeLanguage(msg, 
                command, cancellationToken),
            BotCommands.CommandDashboardApiGetToken => DashBoardApiGetToken(msg),
            BotCommands.CommandPhoto => SendPhoto(msg),
            BotCommands.CommandInlineButtons => SendInlineKeyboard(msg),
            BotCommands.CommandKeyboard => SendReplyKeyboard(msg),
            BotCommands.CommandRemove => RemoveKeyboard(msg),
            BotCommands.CommandRequest => RequestContactAndLocation(msg),
            BotCommands.CommandInlineMode => StartInlineQuery(msg),
            BotCommands.CommandPoll => SendPoll(msg),
            BotCommands.CommandPollAnonymous => SendAnonymousPoll(msg),
            BotCommands.CommandThrow => FailingHandler(),
            BotCommands.CommandRefreshProfilePhotos => AdminRefreshAllProfilePhotosAsync(msg, cancellationToken),

            _ => Usage(msg, cancellationToken)
        });
    }

    private async Task<Message> Usage(Message msg, CancellationToken cancellationToken)
    {
        return await _botClient.SendMessage(msg.Chat, 
            await GetLocalizationTextAsync("BotMenu", msg.Chat.Id, cancellationToken)
            , parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> Start(Message msg, CancellationToken cancellationToken)
    {
        // Register a new user if applicable
        await RegisterNewUserAsync(msg, cancellationToken);

        return await SelectLanguage(msg, cancellationToken);
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Processing your request...",
            cancellationToken: cancellationToken);

        var data = callbackQuery.Data?.Trim();
        if (string.IsNullOrWhiteSpace(data))
        {
            _logger.LogWarning("Empty callback data received.");
            return;
        }

        var message = callbackQuery.Message ?? throw new InvalidOperationException("Message is null.");
        var lowerData = data.ToLowerInvariant();
        
        await LogIncomingMessage(callbackQuery, cancellationToken);

        if (lowerData.StartsWith($"{BotCommands.CommandDeleteSelectedFile} "))
        {
            var parts = data.Split(' ', 3);
            if (parts.Length == 3)
            {
                var vpnServerId = parts[1];
                var fileName = parts[2];
                _logger.LogInformation("Deleting file: {FileName} from server {ServerId}", fileName, vpnServerId);
                await DeleteFile(callbackQuery.From.Id, vpnServerId, fileName, cancellationToken);
            }else if (parts.Length == 2)
            {
                var vpnServerId = data.Substring(BotCommands.CommandDeleteSelectedFile.Length + 1);
                _logger.LogInformation("Delete selected file for vpnServerId: {VpnServerId}", vpnServerId);
                await DeleteSelectedFile(message, vpnServerId, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Invalid delete_file callback format: {Data}", data);
            }
        }
        else if (lowerData.StartsWith($"{BotCommands.CommandGetMyFiles} "))
        {
            var vpnServerId = data.Substring(BotCommands.CommandGetMyFiles.Length + 1);
            _logger.LogInformation("Get files for vpnServerId: {VpnServerId}", vpnServerId);
            await GetMyFiles(message, vpnServerId, cancellationToken);
        }
        else if (lowerData.StartsWith($"{BotCommands.CommandGetMyFilesWithToken} "))
        {
            var vpnServerId = data.Substring(BotCommands.CommandGetMyFilesWithToken.Length + 1);
            _logger.LogInformation("Get files for vpnServerId: {VpnServerId}", vpnServerId);
            await GetMyFilesWithToken(message, vpnServerId, cancellationToken);
        }
        else if (lowerData.StartsWith($"{BotCommands.CommandMakeNewFile} "))
        {
            var vpnServerId = data.Substring(BotCommands.CommandMakeNewFile.Length + 1);
            _logger.LogInformation("Make new file for vpnServerId: {VpnServerId}", vpnServerId);
            await MakeNewVpnFile(message, vpnServerId, cancellationToken);
        }
        else if (lowerData.StartsWith($"{BotCommands.CommandMakeNewFileWithToken} "))
        {
            var vpnServerId = data.Substring(BotCommands.CommandMakeNewFileWithToken.Length + 1);
            _logger.LogInformation("Make new file with token for vpnServerId: {VpnServerId}", vpnServerId);
            await MakeNewVpnFileWithToken(message, vpnServerId, cancellationToken);
        }
        else if (lowerData.StartsWith($"{BotCommands.CommandDeleteAllFiles} "))
        {
            var vpnServerId = data.Substring(BotCommands.CommandDeleteAllFiles.Length + 1);
            _logger.LogInformation("Delete all files for vpnServerId: {VpnServerId}", vpnServerId);
            await DeleteAllFiles(message, vpnServerId, cancellationToken);
        }
        else if (data is BotCommands.CommandEnglish or BotCommands.CommandRussian or BotCommands.CommandGreek)
        {
            _logger.LogInformation("User selected language: {Language}", data);
            await ChangeLanguage(message, data.ToLowerInvariant(), cancellationToken);
        }
        else
        {
            _logger.LogWarning("Invalid callback data received: {CallbackData}", data);
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid callback data received. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<Message> RegisterForVpn(Message msg, CancellationToken cancellationToken)
    {
        if (msg.From != null)
            await RegisterNewUserAsync(msg, cancellationToken);

        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            await GetLocalizationTextAsync("Registered", msg.From!.Id, cancellationToken),
            cancellationToken: cancellationToken);
    }

    private async Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        var updateDetails = JsonConvert.SerializeObject(update, Formatting.Indented);

        var ex = new InvalidOperationException(
            $"Unknown update type received: {update.Type}\nDetails:\n{updateDetails}");

        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);

        _logger.LogWarning("⚠️ Unknown update sent to admin: {UpdateType}\n{Details}", update.Type, updateDetails);
    }

    private async Task OnUnsupportedChannelUpdateAsync(string updateType, Message message, CancellationToken cancellationToken)
    {
        var chat = DescribeChat(message.Chat);
        var actor = DescribeUser(message.From);
        var payload = string.IsNullOrWhiteSpace(message.Text)
            ? "<empty>"
            : message.Text.Length > 500
                ? message.Text[..500] + "... (truncated)"
                : message.Text;

        var text =
            "ℹ️ Unsupported Telegram update received\n" +
            $"Type: {updateType}\n" +
            "Status: currently not supported by this bot\n" +
            $"Chat: {chat}\n" +
            $"Actor: {actor}\n" +
            $"MessageId: {message.Id}\n" +
            $"Payload: {payload}\n" +
            $"Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        _logger.LogInformation("Unsupported update {UpdateType} received. Chat={Chat}; MessageId={MessageId}",
            updateType, chat, message.Id);

        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.SendMessageToAdminsAsync(text, cancellationToken);
    }

    private async Task OnMyChatMemberUpdate(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        var actor = DescribeUser(update.From);
        var target = DescribeUser(update.NewChatMember?.User);
        var chat = DescribeChat(update.Chat);
        var oldStatus = update.OldChatMember?.Status.ToString() ?? "Unknown";
        var newStatus = update.NewChatMember?.Status.ToString() ?? "Unknown";
        var eventTime = update.Date == default ? DateTimeOffset.UtcNow : update.Date;

        var text =
            "ℹ️ Telegram chat-member update\n" +
            $"Type: MyChatMember\n" +
            $"Chat: {chat}\n" +
            $"Actor: {actor}\n" +
            $"Target: {target}\n" +
            $"Status: {oldStatus} -> {newStatus}\n" +
            $"Time: {eventTime:yyyy-MM-dd HH:mm:ss} UTC";

        _logger.LogInformation("MyChatMember update received. Chat={Chat}; Actor={Actor}; Target={Target}; {Old}->{New}",
            chat, actor, target, oldStatus, newStatus);

        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.SendMessageToAdminsAsync(text, cancellationToken);
    }

    private async Task OnChatMemberUpdate(ChatMemberUpdated update, CancellationToken cancellationToken)
    {
        var actor = DescribeUser(update.From);
        var target = DescribeUser(update.NewChatMember?.User);
        var chat = DescribeChat(update.Chat);
        var oldStatus = update.OldChatMember?.Status.ToString() ?? "Unknown";
        var newStatus = update.NewChatMember?.Status.ToString() ?? "Unknown";
        var eventTime = update.Date == default ? DateTimeOffset.UtcNow : update.Date;

        var text =
            "ℹ️ Telegram chat-member update\n" +
            $"Type: ChatMember\n" +
            $"Chat: {chat}\n" +
            $"Actor: {actor}\n" +
            $"Target: {target}\n" +
            $"Status: {oldStatus} -> {newStatus}\n" +
            $"Time: {eventTime:yyyy-MM-dd HH:mm:ss} UTC";

        _logger.LogInformation("ChatMember update received. Chat={Chat}; Actor={Actor}; Target={Target}; {Old}->{New}",
            chat, actor, target, oldStatus, newStatus);

        using var scope = _serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        await errorService.SendMessageToAdminsAsync(text, cancellationToken);
    }

    private static string DescribeUser(User? user)
    {
        if (user == null)
            return "Unknown";
        var username = string.IsNullOrWhiteSpace(user.Username) ? "" : $"@{user.Username}";
        return $"{user.FirstName} {user.LastName}".Trim() + $" (id={user.Id}) {username}".TrimEnd();
    }

    private static string DescribeChat(Chat? chat)
    {
        if (chat == null)
            return "Unknown";
        var titleOrName = !string.IsNullOrWhiteSpace(chat.Title)
            ? chat.Title
            : $"{chat.FirstName} {chat.LastName}".Trim();
        var username = string.IsNullOrWhiteSpace(chat.Username) ? "" : $"@{chat.Username}";
        return $"{titleOrName} (id={chat.Id}, type={chat.Type}) {username}".TrimEnd();
    }

    private async Task<Message> RegisterButtonsAsync(Message msg, CancellationToken cancellationToken)
    {//todo: for future
        await botClient.SetChatMenuButton(
            menuButton: new MenuButtonWebApp
            {
                Text = "Open VPN App",
                WebApp = new WebAppInfo { Url = "https://yourdomain.com/mini/" }
            },
            cancellationToken: cancellationToken);

        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: "\u2705 Button have been successfully registered...",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task<Message> RegisterCommandsAsync(Message msg, CancellationToken cancellationToken)
    {
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.English), 
            languageCode: "en", cancellationToken: cancellationToken);
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.Russian), 
            languageCode: "ru", cancellationToken: cancellationToken);
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.Greek), 
            languageCode: "el", cancellationToken: cancellationToken);
        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: "\u2705 All commands have been successfully registered...",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }
    
    private async Task RegisterNewUserAsync(Message msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var request = new RegisterUserFromTgBotRequest
        {
            TelegramId = msg.From!.Id,
            FirstName = msg.From.FirstName,
            LastName = msg.From.LastName,
            Username = msg.From.Username,
            LanguageCode = msg.From.LanguageCode,
            IsPremium = msg.From.IsPremium
        };

        if (!await registrationService.UserExistsAsync(request.TelegramId, cancellationToken))
        {
            _logger.LogInformation("User with TelegramId {TelegramId} not found. Registering...", request.TelegramId);
            await registrationService.RegisterUserAsync(request, cancellationToken);
        }
    }

    private async Task LogIncomingMessage(Message msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var incomingMessageLogService = scope.ServiceProvider.GetRequiredService<IIncomingMessageLogService>();
        await incomingMessageLogService.Log(_botClient, msg, cancellationToken);
    }
    
    private async Task LogIncomingMessage(CallbackQuery msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var incomingMessageLogService = scope.ServiceProvider.GetRequiredService<IIncomingMessageLogService>();
        await incomingMessageLogService.Log(_botClient, msg, cancellationToken);
    }
    
    private async Task<bool> IsExistLocalizationSettings(long telegramId, CancellationToken cancellationToken)
    {
        var request = new IsExistTelegramUserLanguagePreferenceRequest() { TelegramId = telegramId };
        
        _logger.LogInformation($"Checking localization settings for TelegramId: {telegramId}.");
        using var scope = _serviceProvider.CreateScope();
        var incomingMessageLogService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var result = 
            await incomingMessageLogService.IsExistTelegramUserLanguagePreferenceAsync(request, cancellationToken);
        _logger.LogInformation($"Result of IsExistUserLanguageAsync for TelegramId {telegramId}: {result}");

        return result.IsExistTelegramUserLanguagePreference;
    }
}