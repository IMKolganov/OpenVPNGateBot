using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class LocalizationServiceTests
{
    [Fact]
    public async Task SetTelegramUserLanguageAsync_Throws_When_TelegramId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse> { Success = true, Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t", Expiration = DateTimeOffset.UtcNow.AddHours(1) } });
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new LocalizationService(Mock.Of<ILogger<LocalizationService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SetTelegramUserLanguageAsync(new SetTelegramUserLanguageRequest { TelegramId = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetTelegramUserLanguageAsync_Throws_When_TelegramId_Zero()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new LocalizationService(Mock.Of<ILogger<LocalizationService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetTelegramUserLanguageAsync(new GetTelegramUserLanguageRequest { TelegramId = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetTextForTelegramUser_Throws_When_Key_Empty()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "c", "s", Mock.Of<ILogger<AuthService>>());
        var sut = new LocalizationService(Mock.Of<ILogger<LocalizationService>>(), httpRequest.Object, authService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetTextForTelegramUser(new GetTextForTelegramUserRequest { TelegramId = 123, Key = "" }, CancellationToken.None));
    }
}
