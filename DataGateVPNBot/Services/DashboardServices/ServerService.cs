using System.Security.Authentication;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class ServerService(
    ILogger<ServerService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService)
{
    private const string EndpointGetAllOpenVpnServers = "api/open-vpn-servers/get-all";
    private const string EndpointGetVpnServer = "api/open-vpn-servers/get";

    public async Task<VpnServerDto?> GetVpnServerByIdAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = $"{EndpointGetVpnServer}/{vpnServerId}";
        var response = await httpRequestService.GetAsync<ApiResponse<VpnServerResponse>>(url, token, cancellationToken);
        if (response is { Success: true, Data: not null })
            return response.Data.VpnServer;

        logger.LogWarning("Failed to get VPN server {VpnServerId}: {Message}", vpnServerId, response?.Message);
        return null;
    }

    public async Task<VpnServersResponse> GetOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");

        var url = EndpointGetAllOpenVpnServers;
        
        var response = await httpRequestService.GetAsync<ApiResponse<VpnServersResponse>>(url, token, cancellationToken);

        if (!(response?.Success == true && response.Data is not null))
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
            if (response == null)
                logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return response!.Data ?? throw new InvalidOperationException("Failed to get VPN servers.");
    }

}