using DataGateVPNBot.Localization;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class AuthService(
    IHttpRequestService httpRequestService,
    string clientId,
    string clientSecret,
    IDashboardAuthAlertService dashboardAuthAlertService,
    ILogger<AuthService> logger)
{
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private DateTime _tokenBlockedUntilUtc = DateTime.MinValue;
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CredentialFailureBackoff = TimeSpan.FromMinutes(30);
    private const string EndpointAuthByToken = "api/auth/token";
    private const string EndpointTelegramRequestLoginCode = "api/auth/telegram/request-login-code";

    public async Task<string?> GetTokenAsync()
    {
        if (DateTime.UtcNow < _tokenBlockedUntilUtc)
        {
            logger.LogWarning(
                "Skipping token request until {UntilUtc:u} due to prior dashboard auth/rate-limit backoff.",
                _tokenBlockedUntilUtc);
            return null;
        }

        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            logger.LogInformation("Using cached token from memory.");
            return _cachedToken;
        }

        logger.LogInformation("Token not found or expired. Requesting new token...");

        var requestBody = new
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        try
        {
            var response = await httpRequestService.PostAsync<ApiResponse<TokenResponse>>(
                EndpointAuthByToken, requestBody);

            if (response == null)
            {
                logger.LogWarning("Empty response from dashboard token endpoint.");
                await HandleTokenFailureAsync("Empty response from dashboard token endpoint.");
                return null;
            }

            if (!response.Success || response.Data == null)
            {
                logger.LogWarning("Token request failed: {Message}", response.Message);
                await HandleTokenFailureAsync(response.Message);
                return null;
            }

            var newToken = response.Data.Token;
            if (string.IsNullOrEmpty(newToken))
            {
                logger.LogWarning("Received empty token string.");
                return null;
            }

            _cachedToken = newToken;

            _tokenExpiry = response.Data.Expiration.UtcDateTime != default
                ? response.Data.Expiration.UtcDateTime
                : DateTime.UtcNow.Add(_tokenExpiration);

            logger.LogInformation("✅ Token cached until {Expiry}", _tokenExpiry);
            return newToken;
        }
        catch (Exception ex)
        {
            await HandleTokenFailureAsync(ex.Message);
            logger.LogError(ex, "❌ Failed to obtain token from API.");
            return null;
        }
    }

    private async Task HandleTokenFailureAsync(string? hint)
    {
        if (IsRateLimitFailure(hint))
        {
            ApplyTokenBackoff(RateLimitBackoff, "rate limiting");
            await dashboardAuthAlertService.TryNotifyAuthFailureAsync(
                hint ?? "Too many token requests. Try again later.");
            return;
        }

        if (IsCredentialFailure(hint))
        {
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            ApplyTokenBackoff(CredentialFailureBackoff, "invalid dashboard credentials");
            await dashboardAuthAlertService.TryNotifyAuthFailureAsync(hint ?? "Authentication rejected.");
            return;
        }

        logger.LogWarning("Dashboard token request failed without classified reason: {Hint}", hint);
        await dashboardAuthAlertService.TryNotifyAuthFailureAsync(hint ?? "Dashboard token request failed.");
    }

    private void ApplyTokenBackoff(TimeSpan duration, string reason)
    {
        _tokenBlockedUntilUtc = DateTime.UtcNow.Add(duration);
        logger.LogWarning(
            "Dashboard token requests paused until {UntilUtc:u} due to {Reason}.",
            _tokenBlockedUntilUtc,
            reason);
    }

    private static bool IsRateLimitFailure(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return false;

        return hint.Contains("Too many token", StringComparison.OrdinalIgnoreCase)
               || hint.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
               || hint.Contains("429", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCredentialFailure(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return false;

        string[] markers =
        [
            "invalid credential",
            "invalid client",
            "invalid secret",
            "unauthorized",
            "forbidden",
            "revoked",
            "application not found",
            "401",
            "403",
        ];

        return markers.Any(marker => hint.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TelegramRequestLoginCodeResponse?> RequestDashboardLoginCodeAsync(long telegramId, CancellationToken ct = default)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Cannot request login code: App token unavailable.");
            return null;
        }

        var body = new TelegramRequestLoginCodeRequest { TelegramId = telegramId };

        try
        {
            var response = await httpRequestService.PostAsync<ApiResponse<TelegramRequestLoginCodeResponse>>(
                EndpointTelegramRequestLoginCode,
                body,
                token,
                ct);

            if (response is not { Success: true, Data: not null })
            {
                logger.LogWarning("Login code request failed for TelegramId {TelegramId}: {Message}",
                    telegramId, response?.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to request dashboard login code for TelegramId {TelegramId}", telegramId);
            return null;
        }
    }

    public async Task<CompleteTelegramAccountLinkResponse?> CompleteAccountLinkAsync(
        string code,
        long telegramId,
        CancellationToken ct = default)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Cannot complete account link: App token unavailable.");
            return null;
        }

        var body = new CompleteTelegramAccountLinkRequest
        {
            Code = code.Trim(),
            TelegramId = telegramId,
        };

        try
        {
            var response = await httpRequestService.PostAsync<ApiResponse<CompleteTelegramAccountLinkResponse>>(
                "api/users/merge-telegram-google/by-link-code",
                body,
                token,
                ct);

            if (response is not { Success: true, Data: not null })
            {
                logger.LogWarning(
                    "Account link failed for TelegramId {TelegramId}: {Message}",
                    telegramId,
                    response?.Message);
                return response?.Data ?? new CompleteTelegramAccountLinkResponse
                {
                    Success = false,
                    Message = response?.Message ?? "Account link request failed.",
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete account link for TelegramId {TelegramId}", telegramId);
            var primary = ApiErrorMessageMapper.ExtractPrimaryMessage(ex.Message);
            if (string.IsNullOrWhiteSpace(primary))
                primary = ex.Message;

            return new CompleteTelegramAccountLinkResponse
            {
                Success = false,
                Message = ApiErrorMessageMapper.TryMap(primary, out _)
                    ? primary
                    : "Could not reach the server. Try again later.",
            };
        }
    }
}
