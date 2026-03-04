using DataGateVPNBot.Controllers;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Controllers;

public class VpnServerControllerTests
{
    [Fact]
    public async Task GetAllVpnServersGet_Returns_Ok_With_List_From_Service()
    {
        var expected = new OpenVpnServersResponse();
        var openVpnServersService = new Mock<IOpenVpnServersService>();
        openVpnServersService.Setup(s => s.GetAllOpenVpnServersListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var errorService = Mock.Of<IErrorService>();
        var controller = new VpnServerController(openVpnServersService.Object, errorService, Mock.Of<ILogger<VpnServerController>>());

        var result = await controller.GetAllVpnServersGet(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Same(expected, apiResponse.Data);
    }

    [Fact]
    public async Task GetAllVpnServersPost_Returns_Ok_With_List_From_Service()
    {
        var expected = new OpenVpnServersResponse();
        var openVpnServersService = new Mock<IOpenVpnServersService>();
        openVpnServersService.Setup(s => s.GetAllOpenVpnServersListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var errorService = Mock.Of<IErrorService>();
        var controller = new VpnServerController(openVpnServersService.Object, errorService, Mock.Of<ILogger<VpnServerController>>());

        var result = await controller.GetAllVpnServersPost(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<OpenVpnServersResponse>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }
}
