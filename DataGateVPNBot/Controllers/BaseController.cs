using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    [HttpGet]
    public IActionResult Healthcheck()
    {
        return Ok(200);
    }

    [HttpGet("HealthcheckWithJwt")]
    [Authorize]
    public IActionResult HealthcheckWithJwt()
    {
        return Ok(new { status = "Healthy" });
    }
}