using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class DashBoardApiOvpnFileService
{
    private readonly ILogger<DashBoardApiOvpnFileService> _logger;
    private readonly IHttpRequestService _httpRequestService;
    private readonly DashBoardApiAuthService _dashBoardApiAuthService;
    private const string EndpointGetAllOpenVpnFiles = "api/OpenVpnFiles/GetAllByExternalIdOvpnFiles";
    private const string EndpointDownloadOpenVpnFiles = "api/OpenVpnFiles/DownloadOvpnFile";
    private const string EndpointAddOpenVpnFile = "api/OpenVpnFiles/AddOvpnFile";
    private const string EndpointRevokeOvpnFile = "api/OpenVpnFiles/RevokeOvpnFile";
    
    public DashBoardApiOvpnFileService(ILogger<DashBoardApiOvpnFileService> logger,
        IHttpRequestService httpRequestService,
        DashBoardApiAuthService dashBoardApiAuthService
        )
    {
        _logger = logger;
        _httpRequestService = httpRequestService;
        _dashBoardApiAuthService = dashBoardApiAuthService;
    }

    public async Task<List<OvpnFileResponse>?> GetAllOvpnFilesByExternalIdAsync(
        int vpnServerId, string externalId, CancellationToken cancellationToken)
    {
        //GetAllByExternalIdOvpnFilesRequest
        if (vpnServerId <= 0) 
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(externalId))
            throw new ArgumentException("externalId is required.");
        
        var token = await _dashBoardApiAuthService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Bearer token.");
            return null;
        }

        var url = $"{EndpointGetAllOpenVpnFiles}?vpnServerId={vpnServerId}&externalId={externalId}";

        _logger.LogInformation($"Requesting OVPN files for Server ID: {vpnServerId}, External ID: {externalId}");
        
        var response = await _httpRequestService.GetAsync<List<OvpnFileResponse>>(url, token, cancellationToken);

        if (response == null)
        {
            _logger.LogError("Failed to fetch OVPN files from API.");
        }

        return response;
    }
    
    public async Task<Stream> DownloadOvpnFileByIdAndServerIdAsync(DownloadOvpnFileRequest request, CancellationToken cancellationToken)
    {
        //DownloadOvpnFileResponse
        if (request.IssuedOvpnFileId <= 0)
            throw new ArgumentException($"Invalid issuedOvpnFileId: {request.IssuedOvpnFileId}. " +
                                        $"Must be greater than zero.", nameof(request.IssuedOvpnFileId));
        
        if (request.VpnServerId <= 0)
            throw new ArgumentException($"Invalid vpnServerId: {request.VpnServerId}. " +
                                        $"Must be greater than zero.", nameof(request.VpnServerId));

        var token = await _dashBoardApiAuthService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Bearer token.");
            throw new InvalidOperationException("Authentication token is required.");
        }

        var url = $"{EndpointDownloadOpenVpnFiles}/{request.IssuedOvpnFileId}/{request.VpnServerId}";

        _logger.LogInformation($"Requesting OVPN file stream for " +
                               $"Issued Ovpn File Id: {request.IssuedOvpnFileId}, Server ID: {request.VpnServerId}");

        var stream = await _httpRequestService.GetStreamAsync(url, token, cancellationToken);//todo:fix

        _logger.LogInformation("OVPN file stream retrieved successfully.");

        return stream;
    }

    public async Task<bool> AddOvpnFileAsync(string telegramId, string commonName, int vpnServerId,
        CancellationToken cancellationToken, string issuedTo = "TelegramBot")
    {
        if (vpnServerId <= 0)
            throw new ArgumentException("vpnServerId is required.");

        if (string.IsNullOrEmpty(telegramId))
            throw new ArgumentException("telegramId is required.");

        if (string.IsNullOrEmpty(commonName))
            throw new ArgumentException("commonName is required.");

        var token = await _dashBoardApiAuthService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Bearer token.");
            return false;
        }

        var requestBody = new AddOvpnFileRequest
        {
            ExternalId = telegramId,
            CommonName = commonName,
            VpnServerId = vpnServerId,
            IssuedTo = issuedTo
        };

        _logger.LogInformation("Sending request to create OVPN file for " +
                               $"TelegramId: {telegramId}, ServerId: {vpnServerId}");

        var response =
            await _httpRequestService.PostAsync<bool>(EndpointAddOpenVpnFile, requestBody, token, cancellationToken);

        if (!response)
        {
            _logger.LogError($"Failed to create OVPN file for User: {telegramId}, ServerId: {vpnServerId}");
        }
        else
        {
            _logger.LogInformation("Successfully created OVPN file for " +
                                   $"TelegramId: {telegramId}, VpnServerId: {vpnServerId}");
        }

        return response;
    }
    
    public async Task<bool> RevokeOvpnFileAsync(RevokeOvpnFileRequest request, int telegramId,
        CancellationToken cancellationToken)
    {
        var token = await _dashBoardApiAuthService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Bearer token.");
            return false;
        }

        _logger.LogInformation("Sending request to revoke OVPN file for " +
                               $"TelegramId: {telegramId}, ServerId: {request.VpnServerId}");

        var response =
            await _httpRequestService.PostAsync<bool>(EndpointRevokeOvpnFile, request, token, cancellationToken);

        if (!response)
        {
            _logger.LogError("Failed to revoke OVPN file for " +
                             $"TelegramId: {telegramId}, ServerId: {request.VpnServerId}, Response: {response}");
        }
        else
        {
            _logger.LogInformation("Successfully revoked OVPN file for " +
                                   $"TelegramId: {telegramId}, ServerId: {request.VpnServerId}, Response: {response}");
        }

        return response;
    }
}