using System.Security.Authentication;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class IncomingMessageLogSenderService(
    ILogger<IncomingMessageLogSenderService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService) : IIncomingMessageLogSenderService
{
    private const string EndpointTelegramBotIncomingMessageLogAddMessage = "api/TelegramBotIncomingMessageLog/AddMessage";

    public async Task<AddMessageResponse> TelegramBotIncomingMessageLogAddMessageAsync(
        AddMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.Message!.TelegramId <= 0)
            throw new ArgumentException("TelegramId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Could not obtain a valid token.");

        logger.LogInformation("Sending message log to API. TelegramId: {TelegramId}", request.Message.TelegramId);

        var response = await httpRequestService.PostAsync<ApiResponse<AddMessageResponse>>(
            EndpointTelegramBotIncomingMessageLogAddMessage, request, token, cancellationToken);

        if (response == null)
        {
            logger.LogError("API response is null when saving message log. TelegramId: {TelegramId}", request.Message.TelegramId);
            throw new Exception("API returned null response.");
        }

        if (!response.Success || response.Data == null)
        {
            logger.LogWarning("API returned error when saving message log. TelegramId: {TelegramId}, Message: {Message}",
                request.Message.TelegramId, response.Message);
            throw new Exception($"API error: {response.Message}");
        }

        logger.LogInformation("Successfully saved message log. TelegramId: {TelegramId}",
            request.Message.TelegramId);

        return response.Data;
    }
}
