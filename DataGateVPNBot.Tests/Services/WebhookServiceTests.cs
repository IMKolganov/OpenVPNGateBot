using System.Net;
using System.Text;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.TelegramApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class WebhookServiceTests
{
    private static BotConfiguration Config(
        string? botToken = "test-token",
        string? hostAddress = "tgbot.example.com",
        int port = 443,
        bool useCertificate = false)
    {
        return new BotConfiguration
        {
            BotToken = botToken ?? "",
            HostAddress = hostAddress ?? "",
            Port = port,
            UseCertificate = useCertificate
        };
    }

    [Fact]
    public void SetWebhookAsync_Throws_When_BotToken_Missing()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(botToken: ""));

        var sut = new WebhookService(client, logger, options);

        var ex = Assert.Throws<NullReferenceException>(() => sut.SetWebhookAsync(CancellationToken.None).GetAwaiter().GetResult());
        Assert.Contains("BotToken", ex.Message);
    }

    [Fact]
    public void SetWebhookAsync_Throws_When_HostAddress_Missing()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: ""));

        var sut = new WebhookService(client, logger, options);

        var ex = Assert.Throws<NullReferenceException>(() => sut.SetWebhookAsync(CancellationToken.None).GetAwaiter().GetResult());
        Assert.Contains("HostAddress", ex.Message);
    }

    [Fact]
    public async Task SetWebhookAsync_With_UseCertificate_False_Sends_Expected_Url_And_No_Certificate()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: "https://tgbot.example.com/", port: 443, useCertificate: false));

        var sut = new WebhookService(client, logger, options);
        await sut.SetWebhookAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("setWebhook", handler.LastRequest.RequestUri?.ToString());

        var body = handler.CapturedRequestBody;
        Assert.NotNull(body);
        var bodyStr = Encoding.UTF8.GetString(body);
        Assert.Contains("https://tgbot.example.com/api/bot", bodyStr);
        Assert.DoesNotContain("datagatetgbot.pem", bodyStr);
    }

    [Fact]
    public async Task SetWebhookAsync_With_Port_Not_443_Includes_Port_In_Url()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: "tgbot.example.com", port: 5050, useCertificate: false));

        var sut = new WebhookService(client, logger, options);
        await sut.SetWebhookAsync(CancellationToken.None);

        var bodyStr = Encoding.UTF8.GetString(handler.CapturedRequestBody!);
        Assert.Contains("https://tgbot.example.com:5050/api/bot", bodyStr);
    }

    [Fact]
    public async Task SetWebhookAsync_When_Api_Returns_Error_Throws()
    {
        var handler = new CaptureHandler(HttpStatusCode.BadRequest, "{\"ok\":false}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config());

        var sut = new WebhookService(client, logger, options);

        var ex = await Assert.ThrowsAsync<Exception>(() => sut.SetWebhookAsync(CancellationToken.None));
        Assert.Contains("Failed to set webhook", ex.Message);
    }

    [Fact]
    public async Task IsWebhookSetAsync_Throws_When_BotToken_Missing()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(botToken: ""));

        var sut = new WebhookService(client, logger, options);

        await Assert.ThrowsAsync<NullReferenceException>(() => sut.IsWebhookSetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IsWebhookSetAsync_Throws_When_HostAddress_Missing()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: ""));

        var sut = new WebhookService(client, logger, options);

        await Assert.ThrowsAsync<NullReferenceException>(() => sut.IsWebhookSetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IsWebhookSetAsync_Returns_True_When_Url_And_Cert_Match()
    {
        var expectedUrl = "https://tgbot.example.com/api/bot";
        var json = $"{{\"ok\":true,\"result\":{{\"url\":\"{expectedUrl}\",\"has_custom_certificate\":false}}}}";
        var handler = new CaptureHandler(HttpStatusCode.OK, json);
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: "tgbot.example.com", port: 443, useCertificate: false));

        var sut = new WebhookService(client, logger, options);
        var result = await sut.IsWebhookSetAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsWebhookSetAsync_Returns_False_When_Url_Mismatch()
    {
        var json = "{\"ok\":true,\"result\":{\"url\":\"https://other.example.com/api/bot\",\"has_custom_certificate\":false}}";
        var handler = new CaptureHandler(HttpStatusCode.OK, json);
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(hostAddress: "tgbot.example.com", port: 443));

        var sut = new WebhookService(client, logger, options);
        var result = await sut.IsWebhookSetAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsWebhookSetAsync_Returns_False_When_Api_Not_Ok()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":false}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config());

        var sut = new WebhookService(client, logger, options);
        var result = await sut.IsWebhookSetAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteWebhookAsync_Throws_When_BotToken_Missing()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config(botToken: ""));

        var sut = new WebhookService(client, logger, options);

        await Assert.ThrowsAsync<NullReferenceException>(() => sut.DeleteWebhookAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DeleteWebhookAsync_Does_Not_Throw_On_Success()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new HttpClient(handler);
        var logger = Mock.Of<ILogger<WebhookService>>();
        var options = Options.Create(Config());

        var sut = new WebhookService(client, logger, options);
        await sut.DeleteWebhookAsync(CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Contains("deleteWebhook", handler.LastRequest!.RequestUri?.ToString());
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public CaptureHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public byte[]? CapturedRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                CapturedRequestBody = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
