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
    private const string EndpointGetAllOpenVpnFiles = "api/open-vpn-servers/get-all";

    public async Task<List<OpenVpnServerResponse>?> GetOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();
        var servers = new List<OpenVpnServerResponse>();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetAllOpenVpnFiles}";
        
        var response = await httpRequestService.GetAsync<ApiResponse<List<OpenVpnServerResponse>>>(url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
            servers = response.Data;
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch Open VPN Servers from API.");
        }

        return servers;
    }
}