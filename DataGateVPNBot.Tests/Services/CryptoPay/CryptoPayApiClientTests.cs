using System.Net;
using System.Text;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.CryptoPay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DataGateVPNBot.Tests.Services.CryptoPay;

public class CryptoPayApiClientTests
{
    [Fact]
    public async Task CreateInvoiceAsync_Posts_To_Api_And_Returns_Invoice()
    {
        HttpRequestMessage? sent = null;
        var handler = new StubHandler((request, _) =>
        {
            sent = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"ok":true,"result":{"invoice_id":11,"bot_invoice_url":"https://t.me/CryptoBot?start=inv"}}""",
                    Encoding.UTF8,
                    "application/json")
            });
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://pay.crypt.bot/") };
        var sut = new CryptoPayApiClient(http, Options.Create(new CryptoPayConfiguration
        {
            Enabled = true,
            ApiToken = "secret-token"
        }), NullLogger<CryptoPayApiClient>.Instance);

        var invoice = await sut.CreateInvoiceAsync(new CryptoPayCreateInvoiceRequest
        {
            Amount = "5",
            Fiat = "USD"
        }, CancellationToken.None);

        Assert.Equal(11, invoice.InvoiceId);
        Assert.Equal("https://t.me/CryptoBot?start=inv", invoice.PayUrl);
        Assert.NotNull(sent);
        Assert.Equal(HttpMethod.Post, sent!.Method);
        Assert.Equal("/api/createInvoice", sent.RequestUri!.AbsolutePath);
        Assert.True(sent.Headers.TryGetValues("Crypto-Pay-API-Token", out var values));
        Assert.Equal("secret-token", values.Single());
    }

    [Fact]
    public async Task CreateInvoiceAsync_Throws_When_Api_Returns_Error()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ok":false,"error":{"code":400,"name":"AMOUNT_TOO_SMALL"}}""")
        }));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://pay.crypt.bot/") };
        var sut = new CryptoPayApiClient(http, Options.Create(new CryptoPayConfiguration
        {
            Enabled = true,
            ApiToken = "token"
        }), NullLogger<CryptoPayApiClient>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateInvoiceAsync(new CryptoPayCreateInvoiceRequest { Amount = "0.001" }, CancellationToken.None));
        Assert.Contains("AMOUNT_TOO_SMALL", ex.Message);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
