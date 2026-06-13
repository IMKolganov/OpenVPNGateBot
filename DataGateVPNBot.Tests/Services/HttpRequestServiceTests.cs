using System.Net;
using System.Text;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class HttpRequestServiceTests
{
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
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}");
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
}
