using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataGateVPNBot.Handlers;

public partial class TelegramUpdateHandler
{
    private async Task<Message> SelectLanguage(Message msg, CancellationToken cancellationToken, string textError = "")
    {
        var inlineKeyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("English", "/english".ToLower()),
                InlineKeyboardButton.WithCallbackData("Русский", "/русский".ToLower()),
                InlineKeyboardButton.WithCallbackData("Ελληνικά", "/ελληνικά".ToLower())
            ]
        ]);

        return await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: textError + "🔹 You can click on your preferred language to proceed.\n" +
                  "🔹 Выберите ваш язык, нажав на соответствующую кнопку.\n" +
                  "🔹 Επιλέξτε τη γλώσσα σας πατώντας το αντίστοιχο κουμπί.",
            replyMarkup: inlineKeyboard, 
            cancellationToken: cancellationToken);
    }

    private async Task<Message> ChangeLanguage(Message msg, string selectedLanguage, 
        CancellationToken cancellationToken)
    {
        Language? language = selectedLanguage.ToLower() switch
        {
            "/english" => Language.English,
            "/русский" => Language.Russian,
            "/ελληνικά" => Language.Greek,
            _ => null
        };

        if (language == null)
        {
            return await SelectLanguage(msg, cancellationToken,"❌ Invalid language selection. Please try again.");
        }

        using var scope = _serviceProvider.CreateScope();
        var request = new SetTelegramUserLanguageRequest()
        {
            TelegramId = msg.Chat.Id, 
            PreferredLanguage = (Language)language
        };
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        await localizationService.SetTelegramUserLanguageAsync(request, cancellationToken);
        
        var messageResponse = await _botClient.SendMessage(
            chatId: msg.Chat.Id,
            text: await GetLocalizationTextAsync("SuccessChangeLanguage", msg.Chat.Id, cancellationToken),
            replyMarkup: new ReplyKeyboardRemove(), 
            cancellationToken: cancellationToken);
        
        var openVpnServersService = scope.ServiceProvider.GetRequiredService<IOpenVpnServersService>();
        var serverResponses = await openVpnServersService.GetAllOpenVpnServersListAsync(cancellationToken);

        var defaultServerId = serverResponses.VpnServers
            .Where(x => x.IsDefault)
            .Select(x => x.Id)
            .FirstOrDefault();

        if (defaultServerId <= 0)
        {
            throw new Exception("No default VPN server found.");
        }

        await MakeNewVpnFileWithToken(msg, defaultServerId.ToString(), cancellationToken);
        await InstallClient(msg, cancellationToken);
        await Usage(msg, cancellationToken);

        return messageResponse;
    }
    
    private async Task<string> GetLocalizationTextAsync(string key, long telegramId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        var request = new GetTextForTelegramUserRequest() { TelegramId = telegramId, Key = key };
        return (await localizationService.GetTextForTelegramUser(request, cancellationToken)).Text;
    }
}