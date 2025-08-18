using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using DataGateVPNBot.Tma;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Controllers;

[TmaAuthorize]
[ApiController]
[Route("api/[controller]")]
public class VpnServerController(
    IOpenVpnServersService openVpnServersService,
    IErrorService errorService,
    ILogger<VpnServerController> logger)
    : ControllerBase
{
    private readonly IErrorService _errorService = errorService;
    private readonly ILogger<VpnServerController> _logger = logger;

    [HttpPost("GetAllVpnServers")]
    public async Task<ActionResult<ApiResponse<List<OpenVpnServerResponse>>>> GetAllVpnServersPost(
        CancellationToken cancellationToken)
    {
        var list = await openVpnServersService.GetAllOpenVpnServersListAsync(cancellationToken);
        return Ok(ApiResponse<List<OpenVpnServerResponse>>.SuccessResponse(list));
    }

    [HttpGet("GetAllVpnServers")]
    public async Task<ActionResult<ApiResponse<List<OpenVpnServerResponse>>>> GetAllVpnServersGet(
        CancellationToken cancellationToken)
    {
        var list = await openVpnServersService.GetAllOpenVpnServersListAsync(cancellationToken);
        return Ok(ApiResponse<List<OpenVpnServerResponse>>.SuccessResponse(list));
    }
}