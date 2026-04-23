using DataGateVPNBot.Controllers;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateVPNBot.OvpnFile.Requests;
using Xunit;

namespace DataGateVPNBot.Tests.Controllers;

public class OvpnFileControllerTests
{
    [Fact]
    public async Task DownloadByToken_When_Token_Empty_Returns_BadRequest()
    {
        var resolver = Mock.Of<IVpnProfileTokenDownloadService>();
        var errorService = Mock.Of<IErrorService>();
        var controller = new OvpnFileController(resolver, errorService, Mock.Of<ILogger<OvpnFileController>>());

        var result = await controller.DownloadByToken(new ByTokenRequest { Token = "" }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Token is required", badRequest.Value);
    }

    [Fact]
    public async Task DownloadByToken_When_Token_Null_Returns_BadRequest()
    {
        var resolver = Mock.Of<IVpnProfileTokenDownloadService>();
        var errorService = Mock.Of<IErrorService>();
        var controller = new OvpnFileController(resolver, errorService, Mock.Of<ILogger<OvpnFileController>>());

        var result = await controller.DownloadByToken(new ByTokenRequest { Token = null! }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DownloadByToken_When_Service_Throws_Returns_BadRequest()
    {
        var resolver = new Mock<IVpnProfileTokenDownloadService>();
        resolver.Setup(s => s.DownloadByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));
        var errorService = new Mock<IErrorService>();
        var controller = new OvpnFileController(resolver.Object, errorService.Object, Mock.Of<ILogger<OvpnFileController>>());

        var result = await controller.DownloadByToken(new ByTokenRequest { Token = "invalid" }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
