using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class ServerService(
    ILogger<ServerService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService)
{
    private const string EndpointGetAllOpenVpnServers = "api/open-vpn-servers/get-all";

    public async Task<OpenVpnServersResponse> GetOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = EndpointGetAllOpenVpnServers;
        
        var response = await httpRequestService.GetAsync<ApiResponse<OpenVpnServersResponse>>(url, token, cancellationToken);

        if (!(response?.Success == true && response.Data is not null))
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
            if (response == null)
                logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return response!.Data ?? throw new InvalidOperationException("Failed to get VPN servers.");
    }

}