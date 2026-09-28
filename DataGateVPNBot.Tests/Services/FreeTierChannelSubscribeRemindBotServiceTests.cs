using System.Security.Authentication;
using DataGateVPNBot.Services.BotServices;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Enums;
using DataGateMonitor.SharedModels.DataGateMonitor.FreeTierEnforcement.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class FreeTierChannelSubscribeRemindBotServiceTests
{
    [Fact]
    public async Task RemindAsync_WhenTargetEmpty_ReturnsUsage()
    {
        var sut = new FreeTierChannelSubscribeRemindBotService(
            new AuthService(Mock.Of<IHttpRequestService>(), "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>()),
            Mock.Of<IHttpRequestService>(MockBehavior.Strict),
            Mock.Of<ILogger<FreeTierChannelSubscribeRemindBotService>>());

        var result = await sut.RemindAsync(
            "  ",
            FreeTierChannelSubscribeRemindChannel.Telegram,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.StartsWith("Usage:", result.Message);
    }

    [Fact]
    public async Task RemindAsync_PostsChannelQuery_Telegram()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = true,
                Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t" },
            });
        http.Setup(h => h.PostAsync<ApiResponse<FreeTierChannelSubscribeRemindResponse>>(
                "api/free-tier-enforcement/remind-channel-subscribe/22?channel=Telegram",
                It.IsAny<object>(),
                "t",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<FreeTierChannelSubscribeRemindResponse>
            {
                Success = true,
                Data = FreeTierChannelSubscribeRemindResponse.Ok(
                    FreeTierChannelSubscribeRemindChannel.Telegram,
                    "✅ Reminder sent",
                    telegramId: 22),
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierChannelSubscribeRemindBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierChannelSubscribeRemindBotService>>());

        var result = await sut.RemindAsync("22", FreeTierChannelSubscribeRemindChannel.Telegram, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("✅ Reminder sent", result.Message);
    }

    [Fact]
    public async Task RemindAsync_PostsChannelQuery_Email()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = true,
                Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t" },
            });
        http.Setup(h => h.PostAsync<ApiResponse<FreeTierChannelSubscribeRemindResponse>>(
                "api/free-tier-enforcement/remind-channel-subscribe/150?channel=Email",
                It.IsAny<object>(),
                "t",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<FreeTierChannelSubscribeRemindResponse>
            {
                Success = true,
                Data = FreeTierChannelSubscribeRemindResponse.Ok(
                    FreeTierChannelSubscribeRemindChannel.Email,
                    "✅ Email sent",
                    userId: 150,
                    email: "a@b.c"),
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierChannelSubscribeRemindBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierChannelSubscribeRemindBotService>>());

        var result = await sut.RemindAsync("150", FreeTierChannelSubscribeRemindChannel.Email, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("a@b.c", result.Email);
    }

    [Fact]
    public async Task RemindAsync_WhenApiFails_ReturnsFailMessage()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = true,
                Data = new DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse { Token = "t" },
            });
        http.Setup(h => h.PostAsync<ApiResponse<FreeTierChannelSubscribeRemindResponse>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                "t",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<FreeTierChannelSubscribeRemindResponse>
            {
                Success = false,
                Message = "User has no email",
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierChannelSubscribeRemindBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierChannelSubscribeRemindBotService>>());

        var result = await sut.RemindAsync("7", FreeTierChannelSubscribeRemindChannel.Email, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("User has no email", result.Message);
    }

    [Fact]
    public async Task RemindAsync_Throws_WhenNoToken()
    {
        var http = new Mock<IHttpRequestService>();
        http.Setup(h => h.PostAsync<ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses.TokenResponse>
            {
                Success = false,
            });

        var auth = new AuthService(http.Object, "c", "s", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());
        var sut = new FreeTierChannelSubscribeRemindBotService(
            auth, http.Object, Mock.Of<ILogger<FreeTierChannelSubscribeRemindBotService>>());

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            sut.RemindAsync("22", FreeTierChannelSubscribeRemindChannel.Telegram, CancellationToken.None));
    }
}
