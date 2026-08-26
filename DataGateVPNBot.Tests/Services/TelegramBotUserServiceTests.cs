using DataGateVPNBot.Services;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.Responses;
using DataGateVPNBot.Services.Interfaces;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class TelegramBotUserServiceTests
{
    private static TelegramBotUserService CreateSut(
        IHttpRequestService httpRequest,
        AuthService authService,
        IErrorService? errorService = null,
        AdminRecipientStore? adminRecipientStore = null)
        => new(
            Mock.Of<ILogger<TelegramBotUserService>>(),
            httpRequest,
            authService,
            errorService ?? Mock.Of<IErrorService>(),
            adminRecipientStore ?? new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>()),
            Mock.Of<ITelegramProfilePhotoDownloader>());

    [Fact]
    public async Task RegisterUserAsync_Throws_When_TelegramId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = CreateSut(httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.RegisterUserAsync(new RegisterUserFromTgBotRequest { TelegramId = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetAdminsAsync_Throws_When_No_Token()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse> { Success = false });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = CreateSut(httpRequest.Object, authService);

        await Assert.ThrowsAsync<System.Security.Authentication.AuthenticationException>(() =>
            sut.GetAdminsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UserExistsAsync_ReturnsFalse_WhenApiReportsNotRegistered_WithoutWarning()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = true,
                Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t" },
            });
        httpRequest.Setup(h => h.GetAsync<ApiResponse<bool>>(
                "api/tgbot-users/check-exists/372608421", "t", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<bool> { Success = true, Data = false, Message = "Success" });

        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = CreateSut(httpRequest.Object, authService);

        var exists = await sut.UserExistsAsync(372608421, CancellationToken.None);

        Assert.False(exists);
    }
}
