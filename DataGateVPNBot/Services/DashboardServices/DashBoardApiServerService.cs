using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class DashBoardApiServerService
{
    private readonly ILogger<DashBoardApiServerService> _logger;
    private readonly IHttpRequestService _httpRequestService;
    private readonly DashBoardApiAuthService _dashBoardApiAuthService;
    private const string EndpointGetAllOpenVpnFiles = "api/OpenVpnServers/GetAllServers";
    
    public DashBoardApiServerService(ILogger<DashBoardApiServerService> logger,
        IHttpRequestService httpRequestService,
        DashBoardApiAuthService dashBoardApiAuthService
        )
    {
        _logger = logger;
        _httpRequestService = httpRequestService;
        _dashBoardApiAuthService = dashBoardApiAuthService;
    }

    public async Task<List<OpenVpnServerResponse>?> GetOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        var token = await _dashBoardApiAuthService.GetTokenAsync();
        var servers = new List<OpenVpnServerResponse>();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetAllOpenVpnFiles}";
        
        var response = await _httpRequestService.GetAsync<ApiResponse<List<OpenVpnServerResponse>>>(url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
            servers = response.Data;
        }
        else
        {
            _logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            _logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return servers;
    }
}