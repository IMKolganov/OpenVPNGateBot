using OpenVPNGateMonitor.SharedModels.OpenVpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOpenVpnServersService
{
    Task<List<OpenVpnServerResponse>> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken);
}