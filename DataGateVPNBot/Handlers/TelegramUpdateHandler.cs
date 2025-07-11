using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler : IUpdateHandler
{
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramSettingsService _telegramSettingsService;
    private readonly AuthService _authService;
    private readonly IErrorService _errorService;
    
    public TelegramUpdateHandler(
        ILogger<TelegramUpdateHandler> logger,
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider,
        ITelegramSettingsService telegramSettingsService,
        AuthService authService,
        IErrorService errorService)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _telegramSettingsService = telegramSettingsService ?? throw new ArgumentNullException(nameof(telegramSettingsService));
        _authService = authService;
        _errorService = errorService;
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
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
            { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll } => OnPoll(poll),
            { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
            // ChannelPost:
            // EditedChannelPost:
            // ShippingQuery:
            // PreCheckoutQuery:
            _ => UnknownUpdateHandlerAsync(update)
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
        var command = commandParts[0].ToLower();
        var argument = commandParts.Length > 1 ? commandParts[1] : null;
        await RegisterNewUserAsync(msg, cancellationToken);//for something wrong when "/start" don't work. This line usually is not a necessary 
        
        if (!await IsExistLocalizationSettings(msg.From!.Id, cancellationToken) && 
            (command != "/start"
             ||command != "/change_language"
             ||command != "/english"
             ||command != "/русский"
             ||command != "/ελληνικά"))
        {
            _logger.LogInformation("Localization settings not found for user with TelegramId: {TelegramId}. Calling SelectLanguage.", msg.From.Id);
            return await SelectLanguage(msg, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Localization settings found for user with TelegramId: {TelegramId}.", msg.From.Id);
        }
        

        return await (command switch
        {
            "/start" => Start(msg, cancellationToken),
            "/about_bot" => AboutBot(msg, cancellationToken),
            "/how_to_use" => HowToUseVpn(msg, cancellationToken),
            "/register" => RegisterForVpn(msg, cancellationToken),
            "/get_my_files" => GetMyFiles(msg, argument, cancellationToken),
            "/make_new_file" => MakeNewVpnFile(msg, argument, cancellationToken),
            "/delete_selected_file" => DeleteSelectedFile(msg, argument, cancellationToken),
            "/delete_all_files" => DeleteAllFiles(msg, argument, cancellationToken),
            "/install_client" => InstallClient(msg, cancellationToken),
            "/about_project" => AboutProject(msg, cancellationToken),
            "/contacts" => Contacts(msg, cancellationToken),
            "/change_language" => SelectLanguage(msg, cancellationToken),
            
            "/register_commands" => RegisterCommandsAsync(msg, cancellationToken),

            "/english" => ChangeLanguage(msg, command, cancellationToken),
            "/русский" => ChangeLanguage(msg, command, cancellationToken),
            "/ελληνικά" => ChangeLanguage(msg, command, cancellationToken),
            
            "/dashboard_api_get_token" => DashBoardApiGetToken(msg),

            "/photo" => SendPhoto(msg),
            "/inline_buttons" => SendInlineKeyboard(msg),
            "/keyboard" => SendReplyKeyboard(msg),
            "/remove" => RemoveKeyboard(msg),
            "/request" => RequestContactAndLocation(msg),
            "/inline_mode" => StartInlineQuery(msg),
            "/poll" => SendPoll(msg),
            "/poll_anonymous" => SendAnonymousPoll(msg),
            "/throw" => FailingHandler(),

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
        // Register new user if applicable
        await RegisterNewUserAsync(msg, cancellationToken);

        return await SelectLanguage(msg, cancellationToken);
    }
    
    private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Processing your request...",
            cancellationToken: cancellationToken);

        if (callbackQuery.Data != null && callbackQuery.Data.ToLower().StartsWith("/delete_file "))
        {
            var parts = callbackQuery.Data.Split(' ', 3);
            if (parts.Length == 3)
            {
                var vpnServerId = parts[1];
                var fileName = parts[2];
                _logger.LogInformation($"Deleting file: {fileName} from server {vpnServerId}");
                await DeleteFile(callbackQuery.From.Id, vpnServerId, fileName, cancellationToken);
            }
            else
            {
                _logger.LogWarning($"Invalid delete_file callback: {callbackQuery.Data}");
            }
        }
        else if (callbackQuery.Data != null && callbackQuery.Data.ToLower().StartsWith("/get_my_files "))
        {
            var vpnServerId = callbackQuery.Data.Substring("/get_my_files ".Length);
            _logger.LogInformation($"Get files for vpnServerId: {vpnServerId}");
            await GetMyFiles(callbackQuery.Message ?? throw new InvalidOperationException("Message is null."), 
                vpnServerId, cancellationToken);
        }else if (callbackQuery.Data != null && callbackQuery.Data.ToLower().StartsWith("/make_new_file "))
        {
            var vpnServerId = callbackQuery.Data.Substring("/make_new_file ".Length);
            _logger.LogInformation($"Make files for vpnServerId: {vpnServerId}");
            await MakeNewVpnFile(callbackQuery.Message ?? throw new InvalidOperationException("Message is null."), 
                vpnServerId, cancellationToken);
        }else if (callbackQuery.Data != null && callbackQuery.Data.ToLower().StartsWith("/delete_selected_file "))
        {
            var vpnServerId = callbackQuery.Data.Substring("/delete_selected_file ".Length);
            _logger.LogInformation($"Delete selected file for vpnServerId: {vpnServerId}");
            await DeleteSelectedFile(callbackQuery.Message ?? throw new InvalidOperationException("Message is null."), 
                vpnServerId, cancellationToken);
        }else if (callbackQuery.Data != null && callbackQuery.Data.ToLower().StartsWith("/delete_all_files "))
        {
            var vpnServerId = callbackQuery.Data.Substring("/delete_all_files ".Length);
            _logger.LogInformation($"Delete all files for vpnServerId: {vpnServerId}");
            await DeleteAllFiles(callbackQuery.Message ?? throw new InvalidOperationException("Message is null."), 
                vpnServerId, cancellationToken);
        }
        else if (callbackQuery.Data != null && (callbackQuery.Data.ToLower() == "/english" || 
                                                callbackQuery.Data.ToLower() == "/русский" ||
                                                callbackQuery.Data.ToLower() == "/ελληνικά"))
        {
            if (callbackQuery.Message != null) await ChangeLanguage(callbackQuery.Message, 
                callbackQuery.Data.ToLower(), cancellationToken);
            _logger.LogInformation("User selected language: {Language}", callbackQuery.Data);
        }
        else
        {
            _logger.LogWarning("Invalid callback data received: {CallbackData}", callbackQuery.Data);

            if (callbackQuery.Message != null)
                await _botClient.SendMessage(
                    chatId: callbackQuery.Message.Chat.Id,
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

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
     
    private async Task<Message> RegisterCommandsAsync(Message msg, CancellationToken cancellationToken)
    {
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.English), languageCode: "en", cancellationToken: cancellationToken);
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.Russian), languageCode: "ru", cancellationToken: cancellationToken);
        await _botClient.SetMyCommands(_telegramSettingsService.GetTelegramMenuByLanguage(Language.Greek), languageCode: "el", cancellationToken: cancellationToken);
        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: "\u2705 All commands have been successfully registered...",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }
    
    private async Task RegisterNewUserAsync(Message msg, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<ITelegramBotUserService>();
        var request = new RegisterUserRequest()
            { 
                TelegramId = msg.From!.Id, 
                FirstName = msg.From.FirstName, 
                LastName = msg.From.LastName, 
                Username = msg.From.Username
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