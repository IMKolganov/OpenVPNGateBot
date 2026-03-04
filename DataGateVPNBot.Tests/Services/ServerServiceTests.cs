using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class ServerServiceTests
{
    [Fact]
    public async Task GetOpenVpnServersListAsync_Throws_When_No_Token()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = false });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new ServerService(Mock.Of<ILogger<ServerService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<System.Security.Authentication.AuthenticationException>(() =>
            sut.GetOpenVpnServersListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetOpenVpnServersListAsync_Returns_Data_When_Api_Success()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) } });
        var expected = new OpenVpnServersResponse();
        httpRequest.Setup(h => h.GetAsync<ApiResponse<OpenVpnServersResponse>>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OpenVpnServersResponse> { Success = true, Data = expected });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new ServerService(Mock.Of<ILogger<ServerService>>(), httpRequest.Object, authService);

        var result = await sut.GetOpenVpnServersListAsync(CancellationToken.None);

        Assert.Same(expected, result);
    }
}
