using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotIncomingMessageLog.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class IncomingMessageLogSenderServiceTests
{
    [Fact]
    public async Task TelegramBotIncomingMessageLogAddMessageAsync_Throws_When_TelegramId_Zero()
    {
        var logger = Mock.Of<ILogger<IncomingMessageLogSenderService>>();
        var httpRequest = new Mock<IHttpRequestService>();
        var authService = new AuthService(httpRequest.Object, "clientId", "clientSecret", Mock.Of<ILogger<AuthService>>());
        var sut = new IncomingMessageLogSenderService(logger, httpRequest.Object, authService);

        var request = new AddMessageRequest
        {
            Message = new MessageDto
            {
                TelegramId = 0,
                MessageText = "test",
                ReceivedAt = DateTime.UtcNow
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.TelegramBotIncomingMessageLogAddMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task TelegramBotIncomingMessageLogAddMessageAsync_Throws_When_Auth_Returns_No_Token()
    {
        var logger = Mock.Of<ILogger<IncomingMessageLogSenderService>>();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = false });
        var authService = new AuthService(httpRequest.Object, "clientId", "clientSecret", Mock.Of<ILogger<AuthService>>());
        var sut = new IncomingMessageLogSenderService(logger, httpRequest.Object, authService);

        var request = new AddMessageRequest
        {
            Message = new MessageDto
            {
                TelegramId = 123,
                MessageText = "test",
                ReceivedAt = DateTime.UtcNow
            }
        };

        await Assert.ThrowsAsync<System.Security.Authentication.AuthenticationException>(() =>
            sut.TelegramBotIncomingMessageLogAddMessageAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task TelegramBotIncomingMessageLogAddMessageAsync_Returns_Data_When_Api_Success()
    {
        var logger = Mock.Of<ILogger<IncomingMessageLogSenderService>>();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "token", Expiration = DateTimeOffset.UtcNow.AddHours(1) }
            });
        var expectedData = new AddMessageResponse();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<AddMessageResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<AddMessageResponse> { Success = true, Data = expectedData });

        var authService = new AuthService(httpRequest.Object, "clientId", "clientSecret", Mock.Of<ILogger<AuthService>>());
        var sut = new IncomingMessageLogSenderService(logger, httpRequest.Object, authService);
        var request = new AddMessageRequest
        {
            Message = new MessageDto
            {
                TelegramId = 123,
                MessageText = "test",
                ReceivedAt = DateTime.UtcNow
            }
        };

        var result = await sut.TelegramBotIncomingMessageLogAddMessageAsync(request, CancellationToken.None);

        Assert.Same(expectedData, result);
    }
}
