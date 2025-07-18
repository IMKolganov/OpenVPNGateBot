using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("openvpn-api")]
public class OpenVpnApiController : ControllerBase
{
    [HttpHead("profile")]
    public IActionResult OpenVpnProfileProbe()
    {
        var h = Request.Headers;
        // Response.Headers.Append("WEBAUTH", "false");

        Response.Headers.Append("Ovpn-WebAuth", "none");
        // Response.Headers.Append("Ovpn-WebAuth-Optional", "FlowerVPN,name=Smartcard SAML authentication,external");

        
        
        
        return Ok(); // Must return 200
    }
    
    [HttpGet("profile")]
    public IActionResult OpenVpnProfilePage()
    {
        var token = Request.Query["token"].ToString();

        if (string.IsNullOrEmpty(token))
            return BadRequest("Missing token");


        // 👉 Save this token somewhere if needed for validation later

        var redirectUri = "openvpn://import-profile/https://gate.rackot.ru/api/OvpnFile/DownloadClientOvpnFile/1/178/tg-1-5767006971-3.ovpn";
        return Redirect($"{redirectUri}");
    }

}