using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using ProfilePhotoRefreshOptions = DataGateVPNBot.Models.Configurations.ProfilePhotoRefreshOptions;

namespace DataGateVPNBot.Services.BotServices;

public sealed class TelegramUserProfilePhotoRefreshService(
    ILogger<TelegramUserProfilePhotoRefreshService> logger,
    ITelegramBotClient botClient,
    ITelegramBotUserService telegramBotUserService,
    IOptions<ProfilePhotoRefreshOptions> options)
    : ITelegramUserProfilePhotoRefreshService
{
    public async Task<ProfilePhotoBatchRefreshResult> RefreshAllFromTelegramAsync(
        CancellationToken cancellationToken)
    {
        var delayMs = Math.Clamp(options.Value.DelayMillisecondsBetweenUsers, 0, 5000);

        var data = await telegramBotUserService.GetAllTelegramUsersAsync(cancellationToken);
        if (data is null)
        {
            logger.LogError("Refresh profile photos: could not load users from API.");
            return new ProfilePhotoBatchRefreshResult
            {
                Errors = new[] { "Failed to load users from dashboard API." }
            };
        }

        var users = data.TelegramBotUsers ?? new List<TelegramBotUserDto>();
        var updated = 0;
        var unchanged = 0;
        var skipped = 0;
        var skippedUnavailable = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (delayMs > 0)
                await Task.Delay(delayMs, cancellationToken);

            try
            {
                var photos = await botClient.GetUserProfilePhotos(user.TelegramId, offset: 0, limit: 1,
                    cancellationToken);
                if (photos.TotalCount == 0 || photos.Photos.Length == 0)
                {
                    skipped++;
                    continue;
                }

                var sizes = photos.Photos[^1];
                if (sizes.Length == 0)
                {
                    skipped++;
                    continue;
                }

                var biggest = sizes[^1];
                await using var ms = new MemoryStream();
                await botClient.GetInfoAndDownloadFile(biggest.FileId, ms, cancellationToken);
                var bytes = ms.ToArray();
                if (bytes.Length == 0)
                {
                    skipped++;
                    continue;
                }

                var request = new UpsertTelegramBotUserProfilePhotoRequest
                {
                    TelegramId = user.TelegramId,
                    ProfilePhotoBase64 = Convert.ToBase64String(bytes),
                    ProfilePhotoMimeType = "image/jpeg",
                    ProfilePhotoFileUniqueId = string.IsNullOrEmpty(biggest.FileUniqueId) ? null : biggest.FileUniqueId
                };

                var upsert = await telegramBotUserService.UpsertProfilePhotoAsync(request, cancellationToken);
                if (upsert is null)
                {
                    failed++;
                    AppendError(errors, user.TelegramId, "API returned no data");
                    continue;
                }

                if (upsert.Updated)
                    updated++;
                else
                    unchanged++;
            }
            catch (Exception ex) when (TelegramProfilePhotoAccessHelper.IsUserUnavailableForBot(ex))
            {
                skippedUnavailable++;
                logger.LogInformation(
                    "Skip profile photo for TelegramId {Id}: user unavailable for bot ({Message})",
                    user.TelegramId, ex.Message);
            }
            catch (ApiRequestException ex)
            {
                failed++;
                AppendError(errors, user.TelegramId, ex.Message);
                logger.LogWarning(ex, "Telegram API error for TelegramId {Id}", user.TelegramId);
            }
            catch (Exception ex)
            {
                failed++;
                AppendError(errors, user.TelegramId, ex.Message);
                logger.LogWarning(ex, "Refresh failed for TelegramId {Id}", user.TelegramId);
            }
        }

        return new ProfilePhotoBatchRefreshResult
        {
            TotalUsers = users.Count,
            Updated = updated,
            Unchanged = unchanged,
            SkippedNoProfilePhoto = skipped,
            SkippedUserUnavailable = skippedUnavailable,
            Failed = failed,
            Errors = errors
        };
    }

    private static void AppendError(List<string> errors, long telegramId, string message)
    {
        if (errors.Count >= 8)
            return;
        var line = $"{telegramId}: {message}";
        if (line.Length > 220)
            line = line[..217] + "...";
        errors.Add(line);
    }
}
