using System.Net;
using System.Text;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class HttpRequestServiceTests
{
    [Fact]
    public async Task PostAsync_Returns_Deserialized_ApiResponse_On_Unauthorized_Without_Retry()
    {
        var json = """{"success":false,"message":"Invalid credentials","data":null}""";
        var handler = new CountingHttpMessageHandler(HttpStatusCode.Unauthorized, json);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var errorService = new Mock<IErrorService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.PostAsync<ApiErrorDto>("https://api.example.com/api/auth/token", new { }, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Invalid credentials", result.Message);
        Assert.Equal(1, handler.SendCount);
    }

    [Fact]
    public async Task PostAsync_Returns_Deserialized_ApiResponse_On_TooManyRequests_Without_Retry()
    {
        var json = """{"success":false,"message":"Too many token requests. Try again later.","data":null}""";
        var handler = new CountingHttpMessageHandler(HttpStatusCode.TooManyRequests, json);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var errorService = new Mock<IErrorService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.PostAsync<ApiErrorDto>("https://api.example.com/api/auth/token", new { }, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal(1, handler.SendCount);
        errorService.Verify(
            e => e.NotifyAdminsAboutExceptionAsync(It.IsAny<Exception>(), It.IsAny<HttpContext?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_Returns_Null_On_Unauthorized_Without_Admin_Notify()
    {
        var handler = new CountingHttpMessageHandler(HttpStatusCode.Unauthorized, """{"success":false,"message":"Unauthorized"}""");
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var errorService = new Mock<IErrorService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.GetAsync<ApiErrorDto>("https://api.example.com/api/users/exists", "stale-bearer", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, handler.SendCount);
        errorService.Verify(
            e => e.NotifyAdminsAboutExceptionAsync(It.IsAny<Exception>(), It.IsAny<HttpContext?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_Returns_Deserialized_Object_When_Response_200()
    {
        var json = """{"id":1,"name":"test"}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var client = new HttpClient(handler);

        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);

        var services = new ServiceCollection();
        var errorService = new Mock<IErrorService>();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.GetAsync<TestDto>("https://api.example.com/foo", "token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task GetAsync_Throws_After_Retries_When_Response_Not_Success()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var services = new ServiceCollection();
        services.AddScoped<IErrorService>(_ => Mock.Of<IErrorService>());
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.GetAsync<TestDto>("https://api.example.com/foo", null, CancellationToken.None));
    }

    [Fact]
    public async Task PostAsync_Returns_Deserialized_ApiResponse_On_BadRequest_Without_Retry()
    {
        var json = """{"success":false,"message":"TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)","data":null}""";
        var handler = new CountingHttpMessageHandler(HttpStatusCode.BadRequest, json);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var errorService = new Mock<IErrorService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => errorService.Object);
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.PostAsync<ApiErrorDto>("https://api.example.com/link", new { }, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("TelegramAlreadyLinkedToGoogle|koz_nik (a@b.c)", result.Message);
        Assert.Equal(1, handler.SendCount);
        errorService.Verify(
            e => e.NotifyAdminsAboutExceptionAsync(It.IsAny<Exception>(), It.IsAny<HttpContext?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostAsync_Returns_Deserialized_ApiResponse_On_Forbidden_Quota_Denial()
    {
        var json = """{"success":false,"message":"VpnServerNotAllowedByQuotaPlan","data":null}""";
        var handler = new CountingHttpMessageHandler(HttpStatusCode.Forbidden, json);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var services = new ServiceCollection();
        services.AddScoped<IErrorService>(_ => Mock.Of<IErrorService>());
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        var result = await sut.PostAsync<ApiErrorDto>("https://api.example.com/ovpn", new { }, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("VpnServerNotAllowedByQuotaPlan", result!.Message);
        Assert.Equal(1, handler.SendCount);
    }

    [Fact]
    public async Task GetStreamAsync_Throws_When_Response_Not_Success()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "");
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var services = new ServiceCollection();
        services.AddScoped<IErrorService>(_ => Mock.Of<IErrorService>());
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.GetStreamAsync("https://api.example.com/file", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetStreamAsync_Returns_Stream_When_Response_200()
    {
        var content = Encoding.UTF8.GetBytes("file content");
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, content);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactoryService>();
        factory.Setup(f => f.CreateDashboardClient()).Returns(client);
        var services = new ServiceCollection();
        services.AddScoped<IErrorService>(_ => Mock.Of<IErrorService>());
        var serviceProvider = services.BuildServiceProvider();

        var sut = new HttpRequestService(factory.Object, serviceProvider, Mock.Of<ILogger<HttpRequestService>>());
        await using var stream = await sut.GetStreamAsync("https://api.example.com/file", null, CancellationToken.None);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var result = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Equal("file content", result);
    }

    private sealed class TestDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class ApiErrorDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = Encoding.UTF8.GetBytes(content);
        }

        public FakeHttpMessageHandler(HttpStatusCode statusCode, byte[] content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }

    private sealed class CountingHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        private readonly byte[] _content = Encoding.UTF8.GetBytes(content);
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }
}
