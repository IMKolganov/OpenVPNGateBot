using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class XrayClientLinksDashboardService(
    ILogger<XrayClientLinksDashboardService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService)
{
    private const string EndpointGetByToken = "api/xray-client-links/by-token";
    private const string EndpointGetAll = "api/xray-client-links/get-all";
    private const string EndpointGetAllWithToken = "api/xray-client-links/get-all-with-token";
    private const string EndpointDownload = "api/xray-client-links/download-file";
    private const string EndpointAdd = "api/xray-client-links/add";
    private const string EndpointAddWithToken = "api/xray-client-links/add-with-token";
    private const string EndpointRevoke = "api/xray-client-links/revoke-file";

    public async Task<IssuedOvpnFileDto?> GetOvpnFileByTokenAsync(ByTokenRequest request,
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
        
        var response = await httpRequestService.GetAsync<ApiResponse<OvpnFileResponse>>(url, token, 
            cancellationToken);
        
        if (response is { Success: true, Data: not null })
        {
            return response.Data.IssuedOvpnFile;
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
        }
        
        throw new Exception("Failed to fetch Open VPN Servers from API.");
    }
    
    public async Task<List<IssuedOvpnFileDto>> GetAllOvpnFilesByExternalIdAsync(
        ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("externalId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = $"{EndpointGetAll}/{request.VpnServerId}/{request.ExternalId}";

        logger.LogInformation("Requesting OVPN files for Server ID: {VpnServerId}, External ID: {ExternalId}",
            request.VpnServerId, request.ExternalId);

        var response = await httpRequestService.GetAsync<ApiResponse<OvpnFilesResponse>>(url, token, cancellationToken);

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
            return [];
        }

        if (response is { Success: true, Data: not null })
        {
            return response.Data.IssuedOvpnFiles;
        }
        logger.LogWarning("Failed to get VPN servers: {Message}", response.Message);
        return [];
    }
    
    public async Task<OvpnFilesWithTokensResponse?> GetAllOvpnFilesByExternalIdWithTokenAsync(
        ByExternalIdAndVpnServerIdRequest request, CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("externalId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = $"{EndpointGetAllWithToken}/{request.VpnServerId}/{request.ExternalId}";

        logger.LogInformation("Requesting OVPN files for Server ID: {VpnServerId}, External ID: {ExternalId}",
            request.VpnServerId, request.ExternalId);

        var response = await httpRequestService.GetAsync<ApiResponse<OvpnFilesWithTokensResponse>>(url, token, 
            cancellationToken);

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
            return null;
        }

        if (response is { Success: true, Data: not null })
        {
            return response.Data;
        }

        logger.LogWarning("Failed to get VPN servers: {Message}", response.Message);
        return null;
    }

    public async Task<DownloadFileResponse> DownloadOvpnFileByIdAndServerIdAsync(DownloadFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IssuedOvpnFileId <= 0)
            throw new ArgumentException(
                $"Invalid issuedOvpnFileId: {request.IssuedOvpnFileId}. Must be greater than zero.",
                nameof(request.IssuedOvpnFileId));

        if (request.VpnServerId <= 0)
            throw new ArgumentException($"Invalid vpnServerId: {request.VpnServerId}. Must be greater than zero.",
                nameof(request.VpnServerId));

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        logger.LogInformation(
            "Requesting OVPN file content for IssuedOvpnFileId: {IssuedOvpnFileId}, Server ID: {VpnServerId}",
            request.IssuedOvpnFileId, request.VpnServerId);

        var response = await httpRequestService.PostAsync<ApiResponse<DownloadFileResponse>>(
            EndpointDownload,
            request,
            token,
            cancellationToken);

        if (response == null || response.Data == null)
            throw new InvalidOperationException("OVPN file response is null or invalid.");

        logger.LogInformation("OVPN file stream constructed successfully.");

        return response.Data;
    }

    public async Task<OvpnFileResponse> AddOvpnFileAsync(AddFileRequest request,
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

        logger.LogInformation("Sending request to create OVPN file for " +
                               $"ExternalId: {request.ExternalId}, VpnServerId: {request.VpnServerId}");

        var response =
            await httpRequestService.PostAsync<ApiResponse<OvpnFileResponse>>(EndpointAdd, 
                request, token, cancellationToken);

        if (response is { Success: true, Data: not null, Data.IssuedOvpnFile.Id: > 0 })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return response!.Data!;
    }
    
    public async Task<OvpnFileWithTokenResponse> AddOvpnFileWithTokenAsync(AddFileRequest request,
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

        logger.LogInformation("Sending request to create OVPN file for " +
                              $"ExternalId: {request.ExternalId}, VpnServerId: {request.VpnServerId}");

        var response =
            await httpRequestService.PostAsync<ApiResponse<OvpnFileWithTokenResponse>>(
                EndpointAddWithToken, request, token, cancellationToken);

        if (response is { Success: true, Data: not null, Data.IssuedOvpnFile.Id: > 0 })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return response!.Data!;
    }
    
    public async Task<OvpnFileResponse> RevokeOvpnFileAsync(RevokeFileRequest request, 
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation("Sending request to revoke OVPN file for " +
                               $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}");

        var response =
            await httpRequestService.PostAsync<ApiResponse<OvpnFileResponse>>(EndpointRevoke, 
                request, token, cancellationToken);
        
        if (response is { Success: true, Data: not null })
        {
            logger.LogInformation("Successfully revoked OVPN file for " +
                                   $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}, " +
                                   $"Response: {response}");
        }
        else
        {
            logger.LogError("Failed to revoke OVPN file for " +
                             $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}, " +
                             $"Response: {response}");
        }

        return response!.Data!;
    }
}