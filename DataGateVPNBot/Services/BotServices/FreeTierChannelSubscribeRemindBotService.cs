using System.Security.Authentication;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Enums;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.BotServices;

public sealed class FreeTierChannelSubscribeRemindBotService(
    AuthService authService,
    IHttpRequestService httpRequestService,
    ILogger<FreeTierChannelSubscribeRemindBotService> logger) : IFreeTierChannelSubscribeRemindBotService
{
    private const string EndpointPrefix = "api/free-tier-enforcement/remind-channel-subscribe/";

    public async Task<FreeTierChannelSubscribeRemindResponse> RemindAsync(
        string target,
        FreeTierChannelSubscribeRemindChannel channel,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return FreeTierChannelSubscribeRemindResponse.Fail(
                channel,
                channel == FreeTierChannelSubscribeRemindChannel.Email
                    ? "Usage: /remind_channel_email <userId>\nEmails: subscribe to channel + link Google/password ↔ Telegram."
                    : "Usage: /remind_channel_subscribe <userId|telegramId>\nSends a Telegram DM to subscribe to the required channel.");
        }

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var endpoint =
            $"{EndpointPrefix}{Uri.EscapeDataString(target.Trim())}?channel={Uri.EscapeDataString(channel.ToString())}";
        var response = await httpRequestService.PostAsync<ApiResponse<FreeTierChannelSubscribeRemindResponse>>(
            endpoint,
            new { },
            token,
            cancellationToken);

        if (response is { Success: true, Data: not null })
            return response.Data;

        var message = response?.Message ?? "null response";
        logger.LogWarning(
            "Channel-subscribe remind API failed for {Target} channel={Channel}: {Message}",
            target,
            channel,
            message);
        return FreeTierChannelSubscribeRemindResponse.Fail(
            channel,
            string.IsNullOrWhiteSpace(message) ? "Failed to send reminder." : message);
    }
}
