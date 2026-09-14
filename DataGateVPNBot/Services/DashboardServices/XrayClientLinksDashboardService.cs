using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class XrayClientLinksDashboardService(
    ILogger<XrayClientLinksDashboardService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService)
{
    private const string EndpointGetByToken = "api/v2/xray-client-links/by-token";
    private const string EndpointByServer = "api/v2/xray-client-links/by-server";
    private const string EndpointDownload = "api/v2/xray-client-links/download";
    private const string EndpointAdd = "api/v2/xray-client-links";
    private const string EndpointAddWithToken = "api/v2/xray-client-links/with-token";
    private const string EndpointRevoke = "api/v2/xray-client-links/revoke";

    public async Task<IssuedXrayClientLinkDto?> GetClientLinkByTokenAsync(GetXrayClientLinkByTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            throw new ArgumentNullException(nameof(request.Token));
        }

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetByToken}/{request.Token}";

        var response = await httpRequestService.GetAsync<ApiResponse<XrayClientLinkResponse>>(url, token,
            cancellationToken);

        if (response is { Success: true, Data: not null })
        {
            return response.Data.IssuedXrayClientLink;
        }

        logger.LogWarning("Failed to get Xray client link: {Message}", response?.Message);

        if (response == null)
        {
            logger.LogError("Failed to fetch Xray client link from API.");
        }

        throw new Exception("Failed to fetch Xray client link from API.");
    }

    public async Task<List<IssuedXrayClientLinkDto>> GetAllClientLinksByExternalIdAsync(
        GetXrayClientLinksByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("externalId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = $"{EndpointByServer}/{request.VpnServerId}/by-external-id/{request.ExternalId}";

        logger.LogInformation("Requesting Xray client links for Server ID: {VpnServerId}, External ID: {ExternalId}",
            request.VpnServerId, request.ExternalId);

        var response = await httpRequestService.GetAsync<ApiResponse<XrayClientLinksResponse>>(url, token,
            cancellationToken);

        if (response == null)
        {
            logger.LogError("Failed to fetch Xray client links from API.");
            return [];
        }

        if (response is { Success: true, Data: not null })
        {
            return response.Data.IssuedXrayClientLinks;
        }

        logger.LogWarning("Failed to get Xray client links: {Message}", response.Message);
        return [];
    }

    public async Task<XrayClientLinksWithTokensResponse?> GetAllClientLinksByExternalIdWithTokenAsync(
        GetXrayClientLinksByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("externalId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = $"{EndpointByServer}/{request.VpnServerId}/by-external-id/{request.ExternalId}/with-tokens";

        logger.LogInformation("Requesting Xray client links for Server ID: {VpnServerId}, External ID: {ExternalId}",
            request.VpnServerId, request.ExternalId);

        var response = await httpRequestService.GetAsync<ApiResponse<XrayClientLinksWithTokensResponse>>(url, token,
            cancellationToken);

        if (response == null)
        {
            logger.LogError("Failed to fetch Xray client links from API.");
            return null;
        }

        if (response is { Success: true, Data: not null })
        {
            return response.Data;
        }

        logger.LogWarning("Failed to get Xray client links: {Message}", response.Message);
        return null;
    }

    public async Task<DownloadXrayClientLinkResponse> DownloadClientLinkByIdAndServerIdAsync(
        DownloadXrayClientLinkRequest request, CancellationToken cancellationToken)
    {
        if (request.IssuedXrayClientLinkId <= 0)
            throw new ArgumentException(
                $"Invalid issuedXrayClientLinkId: {request.IssuedXrayClientLinkId}. Must be greater than zero.",
                nameof(request.IssuedXrayClientLinkId));

        if (request.VpnServerId <= 0)
            throw new ArgumentException($"Invalid vpnServerId: {request.VpnServerId}. Must be greater than zero.",
                nameof(request.VpnServerId));

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        logger.LogInformation(
            "Requesting Xray client link content for IssuedXrayClientLinkId: {IssuedXrayClientLinkId}, Server ID: {VpnServerId}",
            request.IssuedXrayClientLinkId, request.VpnServerId);

        var response = await httpRequestService.PostAsync<ApiResponse<DownloadXrayClientLinkResponse>>(
            EndpointDownload,
            request,
            token,
            cancellationToken);

        if (response == null || response.Data == null)
            throw new InvalidOperationException("Xray client link response is null or invalid.");

        logger.LogInformation("Xray client link stream constructed successfully.");

        return response.Data;
    }

    public async Task<XrayClientLinkResponse> AddClientLinkAsync(AddXrayClientLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("VpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("ExternalId is required.");

        if (string.IsNullOrEmpty(request.CommonName))
            throw new ArgumentException("CommonName is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation(
            "Sending request to create Xray client link for ExternalId: {ExternalId}, VpnServerId: {VpnServerId}",
            request.ExternalId, request.VpnServerId);

        var response =
            await httpRequestService.PostAsync<ApiResponse<XrayClientLinkResponse>>(EndpointAdd,
                request, token, cancellationToken);

        return RequireSuccessData(response, "create Xray client link");
    }

    public async Task<XrayClientLinkWithTokenResponse> AddClientLinkWithTokenAsync(AddXrayClientLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("VpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("ExternalId is required.");

        if (string.IsNullOrEmpty(request.CommonName))
            throw new ArgumentException("CommonName is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation(
            "Sending request to create Xray client link for ExternalId: {ExternalId}, VpnServerId: {VpnServerId}",
            request.ExternalId, request.VpnServerId);

        var response =
            await httpRequestService.PostAsync<ApiResponse<XrayClientLinkWithTokenResponse>>(
                EndpointAddWithToken, request, token, cancellationToken);

        return RequireSuccessData(response, "create Xray client link with token");
    }

    private T RequireSuccessData<T>(ApiResponse<T>? response, string operation)
    {
        if (response is { Success: true, Data: not null })
            return response.Data;

        var message = string.IsNullOrWhiteSpace(response?.Message)
            ? $"Failed to {operation}."
            : response.Message;

        logger.LogWarning("Failed to {Operation}: {Message}", operation, message);
        throw new InvalidOperationException(message);
    }

    public async Task<XrayClientLinkResponse> RevokeClientLinkAsync(RevokeXrayClientLinkRequest request,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation(
            "Sending request to revoke Xray client link for CommonName: {CommonName}, ServerId: {VpnServerId}",
            request.CommonName, request.VpnServerId);

        var response =
            await httpRequestService.PostAsync<ApiResponse<XrayClientLinkResponse>>(EndpointRevoke,
                request, token, cancellationToken);

        return RequireSuccessData(response, "revoke Xray client link");
    }
}
