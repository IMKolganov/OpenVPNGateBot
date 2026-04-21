using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class OpenVpnServersServiceTests
{
    [Fact]
    public async Task GetAllOpenVpnServersListAsync_Returns_Result_From_ServerService()
    {
        var expected = new VpnServersResponse();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) } });
        httpRequest.Setup(h => h.GetAsync<ApiResponse<VpnServersResponse>>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<VpnServersResponse> { Success = true, Data = expected });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var serverService = new ServerService(Mock.Of<ILogger<ServerService>>(), httpRequest.Object, authService);
        var logger = Mock.Of<ILogger<DataGateVPNBot.Services.BotServices.OvpnFileService>>();
        var sut = new OpenVpnServersService(serverService, logger);

        var result = await sut.GetAllOpenVpnServersListAsync(CancellationToken.None);

        Assert.Same(expected, result);
    }
}
