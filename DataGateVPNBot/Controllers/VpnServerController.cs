using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VpnServerController(IOpenVpnServersService openVpnServersService, IErrorService errorService,
    ILogger<VpnServerController> logger) : ControllerBase
{
    /// <summary>
    /// </summary>
    // [HttpGet("/DownloadByToken")]
    // [ValidateTelegramWebApp(nameof(ServersRequest.tgWebAppData))]
    public async Task<ActionResult<ApiResponse<List<OpenVpnServerResponse>>>> GetAllVpnServers(
        CancellationToken cancellationToken)
    {
        var response = await openVpnServersService.GetAllOpenVpnServersListAsync(cancellationToken);
        
        return Ok(ApiResponse<List<OpenVpnServerResponse>>.SuccessResponse(response));
    }
}
