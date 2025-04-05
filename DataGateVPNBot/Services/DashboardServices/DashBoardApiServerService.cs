using DataGateVPNBot.Models.DashBoardApi;
using DataGateVPNBot.Services.Http;

namespace DataGateVPNBot.Services.DashboardServices;

public class DashBoardApiServerService
{
    private readonly ILogger<DashBoardApiOvpnFileService> _logger;
    private readonly IHttpRequestService _httpRequestService;
    private readonly DashBoardApiAuthService _dashBoardApiAuthService;
    private const string EndpointGetAllOpenVpnFiles = "api/OpenVpnFiles/GetAllByExternalIdOvpnFiles";
    
    public DashBoardApiServerService(ILogger<DashBoardApiOvpnFileService> logger,
        IHttpRequestService httpRequestService,
        DashBoardApiAuthService dashBoardApiAuthService
        )
    {
        _logger = logger;
        _httpRequestService = httpRequestService;
        _dashBoardApiAuthService = dashBoardApiAuthService;
    }

    public async Task<List<IssuedOvpnFileResponse>?> GetOpenVpnServersListAsync(
        int vpnServerId, CancellationToken cancellationToken)
    {
        if (vpnServerId <= 0) 
            throw new ArgumentException("vpnServerId is required.");


        var token = await _dashBoardApiAuthService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve Bearer token.");
            return null;
        }

        var url = $"{EndpointGetAllOpenVpnFiles}?vpnServerId={vpnServerId}";

        _logger.LogInformation($"Requesting OVPN files for Server ID: {vpnServerId}, External ID: {externalId}");
        
        var response = await _httpRequestService.GetAsync<List<IssuedOvpnFileResponse>>(url, token, cancellationToken);

        if (response == null)
        {
            _logger.LogError("Failed to fetch OVPN files from API.");
        }

        return response;
    }
}