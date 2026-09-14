using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class XrayClientLinkBotServiceTests
{
    [Fact]
    public async Task Dashboard_GetClientLinkByTokenAsync_Throws_When_Token_Empty()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.GetClientLinkByTokenAsync(new GetXrayClientLinkByTokenRequest { Token = "" }, CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_GetAllClientLinksByExternalIdAsync_Throws_When_VpnServerId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetAllClientLinksByExternalIdAsync(
                new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 0, ExternalId = "ext" },
                CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_GetAllClientLinksByExternalIdAsync_Throws_When_ExternalId_Empty()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetAllClientLinksByExternalIdAsync(
                new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 1, ExternalId = "" },
                CancellationToken.None));
    }

    [Fact]
    public async Task Bot_DownloadClientLinkByTokenAsync_Throws_FileNotFoundException_When_File_Not_Found()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) }
            });
        httpRequest.Setup(h => h.GetAsync<ApiResponse<XrayClientLinkResponse>>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<XrayClientLinkResponse>
            {
                Success = true,
                Data = new XrayClientLinkResponse { IssuedXrayClientLink = null! }
            });

        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var dashboard = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(), httpRequest.Object, authService);
        var sut = new XrayClientLinkBotService(
            dashboard,
            Mock.Of<IErrorService>(),
            Mock.Of<ILogger<XrayClientLinkBotService>>());

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            sut.DownloadClientLinkByTokenAsync("invalid-token", CancellationToken.None));
    }

    [Fact]
    public async Task Bot_GetAllClientLinksListAsync_Filters_Revoked_Files()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) }
            });
        var filesResponse = new XrayClientLinksResponse
        {
            IssuedXrayClientLinks =
            [
                new IssuedXrayClientLinkDto { Id = 1, IsRevoked = true, FileName = "a.txt", VpnServerId = 1 },
                new IssuedXrayClientLinkDto { Id = 2, IsRevoked = false, FileName = "b.txt", VpnServerId = 1 }
            ]
        };
        httpRequest.Setup(h => h.GetAsync<ApiResponse<XrayClientLinksResponse>>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<XrayClientLinksResponse> { Success = true, Data = filesResponse });

        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var dashboard = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(), httpRequest.Object, authService);
        var sut = new XrayClientLinkBotService(
            dashboard,
            Mock.Of<IErrorService>(),
            Mock.Of<ILogger<XrayClientLinkBotService>>());

        var result = await sut.GetAllClientLinksListAsync(1, 12345, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        Assert.False(result[0].IsRevoked);
    }
}
