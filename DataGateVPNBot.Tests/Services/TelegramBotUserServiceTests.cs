using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using DataGateVPNBot.Services.Interfaces;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class TelegramBotUserServiceTests
{
    [Fact]
    public async Task RegisterUserAsync_Throws_When_TelegramId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var errorService = Mock.Of<IErrorService>();
        var sut = new TelegramBotUserService(Mock.Of<ILogger<TelegramBotUserService>>(), httpRequest.Object, authService, errorService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.RegisterUserAsync(new RegisterUserFromTgBotRequest { TelegramId = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetAdminsAsync_Throws_When_No_Token()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses.TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses.TokenResponse> { Success = false });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var errorService = Mock.Of<IErrorService>();
        var sut = new TelegramBotUserService(Mock.Of<ILogger<TelegramBotUserService>>(), httpRequest.Object, authService, errorService);

        await Assert.ThrowsAsync<System.Security.Authentication.AuthenticationException>(() =>
            sut.GetAdminsAsync(CancellationToken.None));
    }
}
