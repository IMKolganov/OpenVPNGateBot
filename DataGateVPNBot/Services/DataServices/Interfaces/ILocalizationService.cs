using DataGateVPNBot.Models.Enums;

namespace DataGateVPNBot.Services.DataServices.Interfaces;

public interface ILocalizationService
{
    Task SetUserLanguageAsync(long telegramId, Language language, CancellationToken cancellationToken);
    Task<Language> GetUserLanguageAsync(long userId, CancellationToken cancellationToken);
    Task<string> GetTextAsync(string key, long telegramId, 
        CancellationToken cancellationToken, Language? language = null);
    Task<bool> IsExistUserLanguageAsync(long telegramId, CancellationToken cancellationToken);
}