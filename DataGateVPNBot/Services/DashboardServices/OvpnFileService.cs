using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class OvpnFileService
{
    private readonly ILogger<OvpnFileService> _logger;
    private readonly IHttpRequestService _httpRequestService;
    private readonly AuthService _authService;
    private const string EndpointGetAllOpenVpnFiles = "api/OpenVpnFiles/GetAllByExternalIdOvpnFiles";
    private const string EndpointDownloadOpenVpnFiles = "api/OpenVpnFiles/DownloadClientOvpnFile";
    private const string EndpointAddOpenVpnFile = "api/OpenVpnFiles/AddClientOvpnFile";
    private const string EndpointRevokeOvpnFile = "api/OpenVpnFiles/RevokeClientOvpnFile";
    
    public OvpnFileService(ILogger<OvpnFileService> logger,
        IHttpRequestService httpRequestService,
        AuthService authService
        )
    {
        _logger = logger;
        _httpRequestService = httpRequestService;
        _authService = authService;
    }

    public async Task<List<IssuedOvpnFileDto>?> GetAllOvpnFilesByExternalIdAsync(
        GetAllByExternalIdOvpnFilesRequest request, CancellationToken cancellationToken)
    {
        var ovpnFiles = new List<IssuedOvpnFileDto>();
        if (request.VpnServerId <= 0) 
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("externalId is required.");
        
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetAllOpenVpnFiles}/{request.VpnServerId}/{request.ExternalId}";

        _logger.LogInformation($"Requesting OVPN files for Server ID: {request.VpnServerId}, " +
                               $"External ID: {request.ExternalId}");
        
        var response = await _httpRequestService.GetAsync<ApiResponse<List<IssuedOvpnFileDto>>>(url, token, 
            cancellationToken);
        if (response is { Success: true, Data: not null })
        {
            ovpnFiles = response.Data;
        }
        else
        {
            _logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            _logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return ovpnFiles;
    }
    
    public async Task<Stream> DownloadOvpnFileByIdAndServerIdAsync(DownloadClientOvpnFileRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.IssuedOvpnFileId <= 0)
            throw new ArgumentException($"Invalid issuedOvpnFileId: {request.IssuedOvpnFileId}. " +
                                        $"Must be greater than zero.", nameof(request.IssuedOvpnFileId));
        
        if (request.VpnServerId <= 0)
            throw new ArgumentException($"Invalid vpnServerId: {request.VpnServerId}. " +
                                        $"Must be greater than zero.", nameof(request.VpnServerId));

        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointDownloadOpenVpnFiles}/{request.IssuedOvpnFileId}/{request.VpnServerId}";

        _logger.LogInformation($"Requesting OVPN file stream for " +
                               $"Issued Ovpn File Id: {request.IssuedOvpnFileId}, Server ID: {request.VpnServerId}");

        var stream = await _httpRequestService.GetStreamAsync(url, token, cancellationToken);//todo:fix

        _logger.LogInformation("OVPN file stream retrieved successfully.");

        return stream;
    }

    public async Task<AddOvpnFileResponse> AddOvpnFileAsync(AddClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VpnServerId <= 0)
            throw new ArgumentException("VpnServerId is required.");

        if (string.IsNullOrEmpty(request.ExternalId))
            throw new ArgumentException("ExternalId is required.");

        if (string.IsNullOrEmpty(request.CommonName))
            throw new ArgumentException("CommonName is required.");

        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        _logger.LogInformation("Sending request to create OVPN file for " +
                               $"ExternalId: {request.ExternalId}, VpnServerId: {request.VpnServerId}");

        var response =
            await _httpRequestService.PostAsync<ApiResponse<AddOvpnFileResponse>>(EndpointAddOpenVpnFile, 
                request, token, cancellationToken);

        if (response is { Success: true, Data: not null, Data.IssuedOvpnFile.Id: > 0 })
        {
        }
        else
        {
            _logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            _logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return response!.Data!;
    }
    
    public async Task<RevokeOvpnFileResponse> RevokeOvpnFileAsync(RevokeClientOvpnFileRequest request, 
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        _logger.LogInformation("Sending request to revoke OVPN file for " +
                               $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}");

        var response =
            await _httpRequestService.PostAsync<ApiResponse<RevokeOvpnFileResponse>>(EndpointRevokeOvpnFile, 
                request, token, cancellationToken);
        
        if (response is { Success: true, Data: not null })
        {
            _logger.LogInformation("Successfully revoked OVPN file for " +
                                   $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}, " +
                                   $"Response: {response}");
        }
        else
        {
            _logger.LogError("Failed to revoke OVPN file for " +
                             $"CommonName: {request.CommonName}, ServerId: {request.VpnServerId}, " +
                             $"Response: {response}");
        }

        return response!.Data!;
    }
}