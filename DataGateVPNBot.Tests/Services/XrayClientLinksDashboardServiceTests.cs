using DataGateVPNBot.Localization;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class XrayClientLinksDashboardServiceTests
{
    [Fact]
    public async Task AddClientLinkWithTokenAsync_Throws_With_Api_Message_On_Quota_Denial()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<XrayClientLinkWithTokenResponse>>(
                "api/v2/xray-client-links/with-token",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<XrayClientLinkWithTokenResponse>
            {
                Success = false,
                Message = ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey,
                Data = null,
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddClientLinkWithTokenAsync(new AddXrayClientLinkRequest
            {
                VpnServerId = 77,
                ExternalId = "123",
                CommonName = "cn",
            }, CancellationToken.None));

        Assert.Equal(ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey, ex.Message);
        Assert.True(ApiErrorMessageMapper.TryMap(ex.Message, out var mapped));
        Assert.Equal(ApiErrorMessageMapper.LocalizationVpnServerNotAllowedByQuotaPlan, mapped.LocalizationKey);
    }

    [Fact]
    public async Task AddClientLinkAsync_Throws_With_Api_Message_On_Quota_Denial()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<XrayClientLinkResponse>>(
                "api/v2/xray-client-links",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<XrayClientLinkResponse>
            {
                Success = false,
                Message = ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey,
                Data = null,
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddClientLinkAsync(new AddXrayClientLinkRequest
            {
                VpnServerId = 77,
                ExternalId = "123",
                CommonName = "cn",
            }, CancellationToken.None));

        Assert.Equal(ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey, ex.Message);
    }

    [Fact]
    public async Task GetAllClientLinksByExternalIdAsync_ReturnsIssuedXrayClientLinks()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.GetAsync<ApiResponse<XrayClientLinksResponse>>(
                "api/v2/xray-client-links/by-server/1/by-external-id/ext", "jwt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<XrayClientLinksResponse>
            {
                Success = true,
                Data = new XrayClientLinksResponse
                {
                    IssuedXrayClientLinks =
                    [
                        new() { Id = 5, FileName = "a.txt", VpnServerId = 1 }
                    ]
                }
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var result = await sut.GetAllClientLinksByExternalIdAsync(
            new GetXrayClientLinksByExternalIdAndVpnServerIdRequest { VpnServerId = 1, ExternalId = "ext" },
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(5, result[0].Id);
    }

    [Fact]
    public async Task DownloadClientLinkByIdAndServerIdAsync_Posts_IssuedXrayClientLinkId()
    {
        object? posted = null;
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<DownloadXrayClientLinkResponse>>(
                "api/v2/xray-client-links/download",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .Callback<string, object, string?, CancellationToken>((_, data, _, _) => posted = data)
            .ReturnsAsync(new ApiResponse<DownloadXrayClientLinkResponse>
            {
                Success = true,
                Data = new DownloadXrayClientLinkResponse
                {
                    Content = [1],
                    FileSizeBytes = 1,
                    IssuedXrayClientLink = new() { Id = 42, FileName = "link.txt" }
                }
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var result = await sut.DownloadClientLinkByIdAndServerIdAsync(
            new DownloadXrayClientLinkRequest { IssuedXrayClientLinkId = 42, VpnServerId = 7 },
            CancellationToken.None);

        Assert.Equal(42, result.IssuedXrayClientLink.Id);
        var request = Assert.IsType<DownloadXrayClientLinkRequest>(posted);
        Assert.Equal(42, request.IssuedXrayClientLinkId);
        Assert.Equal(7, request.VpnServerId);
    }
}
