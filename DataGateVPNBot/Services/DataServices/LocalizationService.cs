using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.UnitOfWork;
using DataGateVPNBot.Models;
using DataGateVPNBot.Models.Enums;
using DataGateVPNBot.Services.DataServices.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataGateVPNBot.Services.DataServices;

public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public LocalizationService(IUnitOfWork unitOfWork, ILogger<LocalizationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetUserLanguageAsync(long telegramId, Language language, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Attempting to set language for TelegramId: {telegramId} to {language}.");

        var userLanguagePreferenceRepository = _unitOfWork.GetRepository<UserLanguagePreference>();
        var userPreference = await userLanguagePreferenceRepository.Query
            .FirstOrDefaultAsync(x => x.TelegramId == telegramId, 
                cancellationToken: cancellationToken);

        if (userPreference == null)
        {
            _logger.LogInformation($"No existing language preference found for TelegramId: {telegramId}. " +
                                   $"Creating a new record.", telegramId);

            userPreference = new UserLanguagePreference
            {
                TelegramId = telegramId,
                PreferredLanguage = language
            };
            await userLanguagePreferenceRepository.AddAsync(userPreference, cancellationToken);

            _logger.LogInformation($"New language preference created for TelegramId: {telegramId} " +
                                   $"with language: {language}.");
        }
        else
        {
            _logger.LogInformation($"Existing language preference found for TelegramId: {telegramId}. " +
                                   $"Updating language to: {language}.");

            userPreference.PreferredLanguage = language;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Language preference saved for TelegramId: {TelegramId}.", telegramId);
    }

    public async Task<Language> GetUserLanguageAsync(long telegramId, CancellationToken cancellationToken)
    {
        var userPreference = await _unitOfWork.GetQuery<UserLanguagePreference>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.TelegramId == telegramId, cancellationToken: cancellationToken);

        return userPreference?.PreferredLanguage ?? Language.English;
    }

    public async Task<string> GetTextAsync(string key, long telegramId, 
        CancellationToken cancellationToken, Language? language = null)
    {
        if (language == null)
        {
            language = await GetUserLanguageAsync(telegramId, cancellationToken);
        }

        var text = await _unitOfWork.GetQuery<LocalizationText>()
            .AsQueryable()
            .Where(x => x.Key == key && x.Language == language)
            .Select(x => x.Text)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return text ?? $"[Translation missing for key: {key}, language: {language}]";
    }
    
    public async Task<bool> IsExistUserLanguageAsync(long telegramId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking database for TelegramId: {TelegramId}.", telegramId);

        var userLanguagePreference = await _unitOfWork.GetQuery<UserLanguagePreference>()
            .AsQueryable().AnyAsync(x => x.TelegramId == telegramId,  cancellationToken);

        _logger.LogInformation($"Database check for TelegramId {telegramId}: {userLanguagePreference}");

        return userLanguagePreference;
    }
}