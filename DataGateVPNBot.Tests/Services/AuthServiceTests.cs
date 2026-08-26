using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Responses;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task GetTokenAsync_Notifies_On_Rate_Limit()
    {
        var alert = new Mock<IDashboardAuthAlertService>();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = false,
                Message = "Too many token requests. Try again later.",
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", alert.Object, Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Null(result);
        alert.Verify(
            a => a.TryNotifyAuthFailureAsync("Too many token requests. Try again later.", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_Notifies_And_Backoffs_On_Revoked_Credentials()
    {
        var alert = new Mock<IDashboardAuthAlertService>();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = false,
                Message = "Invalid credentials",
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", alert.Object, Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Null(first);
        Assert.Null(second);
        alert.Verify(a => a.TryNotifyAuthFailureAsync("Invalid credentials", It.IsAny<CancellationToken>()), Times.Once);
        httpRequest.Verify(
            h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_Suppresses_Requests_During_Rate_Limit_Backoff()
    {
        var callCount = 0;
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new ApiResponse<TokenResponse>
                {
                    Success = false,
                    Message = "Too many token requests. Try again later.",
                };
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Null_When_Api_Returns_Unsuccessful()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = false, Message = "Invalid credentials" });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

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
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Equal("jwt-token-123", result);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Null_When_Api_Returns_Empty_Data()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = true, Data = null });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Null_When_Api_Returns_Empty_Token_String()
    {
        var alert = new Mock<IDashboardAuthAlertService>();
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", alert.Object, Mock.Of<ILogger<AuthService>>());

        var result = await sut.GetTokenAsync();

        Assert.Null(result);
        alert.Verify(
            a => a.TryNotifyAuthFailureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_Keeps_Cached_Token_Without_Revalidating_On_Subsequent_Calls()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "cached-token", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Equal("cached-token", first);
        Assert.Equal("cached-token", second);
        httpRequest.Verify(
            h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_Returns_Null_Immediately_During_Backoff()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = false,
                Message = "Invalid credentials",
            });
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Null(first);
        Assert.Null(second);
        httpRequest.Verify(
            h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
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
        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var first = await sut.GetTokenAsync();
        var second = await sut.GetTokenAsync();

        Assert.Equal("cached-token", first);
        Assert.Equal("cached-token", second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task CompleteAccountLinkAsync_ReturnsNull_WhenTokenUnavailable()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse> { Success = false });

        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.CompleteAccountLinkAsync("ABCD2345", 12345, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteAccountLinkAsync_ReturnsResponse_WhenApiSucceeds()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<CompleteTelegramAccountLinkResponse>>(
                "api/users/merge-telegram-google/by-link-code",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<CompleteTelegramAccountLinkResponse>
            {
                Success = true,
                Data = new CompleteTelegramAccountLinkResponse { Success = true, Message = "Linked" },
            });

        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.CompleteAccountLinkAsync("ABCD2345", 12345, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.Success);
    }

    [Fact]
    public async Task CompleteAccountLinkAsync_Returns_Keyed_Message_WhenApiReturns_BadRequest_Body()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<CompleteTelegramAccountLinkResponse>>(
                "api/users/merge-telegram-google/by-link-code",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<CompleteTelegramAccountLinkResponse>
            {
                Success = false,
                Message = "TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)",
                Data = null,
            });

        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.CompleteAccountLinkAsync("ABCD2345", 12345, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)", result.Message);
    }

    [Fact]
    public async Task CompleteAccountLinkAsync_Extracts_Keyed_Message_From_HttpRequestException()
    {
        var httpRequest = new Mock<IHttpRequestService>();
        httpRequest.Setup(h => h.PostAsync<ApiResponse<TokenResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<TokenResponse>
            {
                Success = true,
                Data = new TokenResponse { Token = "jwt", Expiration = DateTimeOffset.UtcNow.AddHours(1) },
            });
        httpRequest.Setup(h => h.PostAsync<ApiResponse<CompleteTelegramAccountLinkResponse>>(
                "api/users/merge-telegram-google/by-link-code",
                It.IsAny<object>(),
                "jwt",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(
                """
                Failed to complete HTTP request to api/users/merge-telegram-google/by-link-code after 3 attempts.
                Attempt 1: BadRequest - Bad Request
                Response body: {"success":false,"message":"TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)","data":null}
                """));

        var sut = new AuthService(httpRequest.Object, "clientId", "secret", Mock.Of<IDashboardAuthAlertService>(), Mock.Of<ILogger<AuthService>>());

        var result = await sut.CompleteAccountLinkAsync("ABCD2345", 12345, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)", result.Message);
    }
}
