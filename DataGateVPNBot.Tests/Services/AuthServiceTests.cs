using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task GetTokenAsync_Returns_Null_When_Api_Returns_Unsuccessful()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = false, Message = "Invalid credentials" });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Token_When_Api_Returns_Success()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt-token-123", Expiration = DateTimeOffset.UtcNow.AddHours(1) }
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Equal("jwt-token-123", result);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Null_When_Api_Returns_Empty_Data()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = null });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTokenAsync_Caches_Token_On_Second_Call()
    {
        var callCount = 0;
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new ApiResponse<TokenResponse>
                {
                    Success = true,
                    Data = new TokenResponse { Token = "cached-token", Expiration = DateTimeOffset.UtcNow.AddHours(1) }
                };
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Equal("cached-token", first);
        Assert.Equal("cached-token", second);
        Assert.Equal(1, callCount);
    }
}
