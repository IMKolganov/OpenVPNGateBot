using DataGateVPNBot.Localization;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class XrayClientLinksDashboardServiceTests
{
    [Fact]
    public async Task AddOvpnFileWithTokenAsync_Throws_With_Api_Message_On_Quota_Denial()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<OvpnFileWithTokenResponse>>(
                "api/xray-client-links/add-with-token",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OvpnFileWithTokenResponse>
            {
                Success = false,
                Message = ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey,
                Data = null,
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddOvpnFileWithTokenAsync(new AddFileRequest
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
    public async Task AddOvpnFileAsync_Throws_With_Api_Message_On_Quota_Denial()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<OvpnFileResponse>>(
                "api/xray-client-links/add",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OvpnFileResponse>
            {
                Success = false,
                Message = ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey,
                Data = null,
            });

        var auth = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new XrayClientLinksDashboardService(
            Mock.Of<ILogger<XrayClientLinksDashboardService>>(),
            httpRequest.Object,
            auth);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddOvpnFileAsync(new AddFileRequest
            {
                VpnServerId = 77,
                ExternalId = "123",
                CommonName = "cn",
            }, CancellationToken.None));

        Assert.Equal(ApiErrorMessageMapper.VpnServerNotAllowedByQuotaPlanKey, ex.Message);
    }
}
