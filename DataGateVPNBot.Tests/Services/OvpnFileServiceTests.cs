using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;
using OpenVPNGateMonitor.SharedModels.Responses;
using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;
using BotServices = DataGateVPNBot.Services.BotServices;
using DashboardOvpnFileService = DataGateVPNBot.Services.DashboardServices.OvpnFileService;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class OvpnFileServiceTests
{
    [Fact]
    public async Task Dashboard_GetOvpnFileByTokenAsync_Throws_When_Token_Empty()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new DashboardOvpnFileService(
            Mock.Of<ILogger<DashboardOvpnFileService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.GetOvpnFileByTokenAsync(new ByTokenRequest { Token = "" }, CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_GetAllOvpnFilesByExternalIdAsync_Throws_When_VpnServerId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new DashboardOvpnFileService(
            Mock.Of<ILogger<DashboardOvpnFileService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetAllOvpnFilesByExternalIdAsync(new ByExternalIdAndVpnServerIdRequest { VpnServerId = 0, ExternalId = "ext" }, CancellationToken.None));
    }

    [Fact]
    public async Task Dashboard_GetAllOvpnFilesByExternalIdAsync_Throws_When_ExternalId_Empty()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new DashboardOvpnFileService(
            Mock.Of<ILogger<DashboardOvpnFileService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetAllOvpnFilesByExternalIdAsync(new ByExternalIdAndVpnServerIdRequest { VpnServerId = 1, ExternalId = "" }, CancellationToken.None));
    }

    [Fact]
    public async Task Bot_DownloadOvpnFileByTokenAsync_Throws_FileNotFoundException_When_File_Not_Found()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) } });
        httpRequest.Setup(h => h.GetAsync<ApiResponse<OvpnFileResponse>>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OvpnFileResponse> { Success = true, Data = new OvpnFileResponse { IssuedOvpnFile = null! } });

        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var dashboardOvpn = new DashboardOvpnFileService(
            Mock.Of<ILogger<DashboardOvpnFileService>>(), httpRequest.Object, authService);
        var errorService = Mock.Of<IErrorService>();
        var sut = new BotServices.OvpnFileService(dashboardOvpn, errorService, Mock.Of<ILogger<BotServices.OvpnFileService>>());

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            sut.DownloadOvpnFileByTokenAsync("invalid-token", CancellationToken.None));
    }

    [Fact]
    public async Task Bot_GetAllOvpnFilesListAsync_Filters_Revoked_Files()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = new TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) } });
        var revokedFile = new IssuedOvpnFileDto { Id = 1, IsRevoked = true, FileName = "a.ovpn", VpnServerId = 1 };
        var validFile = new IssuedOvpnFileDto { Id = 2, IsRevoked = false, FileName = "b.ovpn", VpnServerId = 1 };
        var filesResponse = new OvpnFilesResponse { IssuedOvpnFiles = [revokedFile, validFile] };
        httpRequest.Setup(h => h.GetAsync<ApiResponse<OvpnFilesResponse>>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OvpnFilesResponse> { Success = true, Data = filesResponse });

        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var dashboardOvpn = new DashboardOvpnFileService(
            Mock.Of<ILogger<DashboardOvpnFileService>>(), httpRequest.Object, authService);
        var errorService = Mock.Of<IErrorService>();
        var sut = new BotServices.OvpnFileService(dashboardOvpn, errorService, Mock.Of<ILogger<BotServices.OvpnFileService>>());

        var result = await sut.GetAllOvpnFilesListAsync(1, 12345, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        Assert.False(result[0].IsRevoked);
    }
}
