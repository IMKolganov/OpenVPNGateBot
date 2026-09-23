using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.CryptoPay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services.CryptoPay;

public class CryptoPayDonationServiceTests
{
    [Fact]
    public async Task CreateDonationInvoiceAsync_Sends_Fiat_Payload_And_Paid_Button()
    {
        var api = new Mock<ICryptoPayApiClient>();
        CryptoPayCreateInvoiceRequest? captured = null;
        api.Setup(x => x.CreateInvoiceAsync(It.IsAny<CryptoPayCreateInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CryptoPayCreateInvoiceRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new CryptoPayInvoice { InvoiceId = 7, BotInvoiceUrl = "https://t.me/CryptoBot?start=pay" });

        var sut = CreateSut(api.Object);
        var invoice = await sut.CreateDonationInvoiceAsync(42, 5, "DataGateVPNBot", CancellationToken.None);

        Assert.Equal(7, invoice.InvoiceId);
        Assert.NotNull(captured);
        Assert.Equal("fiat", captured!.CurrencyType);
        Assert.Equal("USD", captured.Fiat);
        Assert.Equal("5", captured.Amount);
        Assert.Equal("tg:42", captured.Payload);
        Assert.Equal("callback", captured.PaidBtnName);
        Assert.Equal("https://t.me/DataGateVPNBot", captured.PaidBtnUrl);
        Assert.Equal("USDT", captured.SwapTo);
    }

    [Fact]
    public async Task ProcessWebhookAsync_Returns_Paid_Donation_Once()
    {
        var config = ConfiguredOptions();
        var token = config.Value.ApiToken;
        var body =
            """{"update_id":1,"update_type":"invoice_paid","payload":{"invoice_id":99,"paid_asset":"USDT","paid_amount":"5.00","payload":"tg:42","comment":"thanks"}}""";
        var signature = CryptoPayWebhookSignature.ComputeHex(token, body);
        var sut = CreateSut(Mock.Of<ICryptoPayApiClient>(), config);

        var first = await sut.ProcessWebhookAsync(body, signature, CancellationToken.None);
        var second = await sut.ProcessWebhookAsync(body, signature, CancellationToken.None);

        Assert.NotNull(first);
        Assert.False(first!.IsDuplicate);
        Assert.Equal(42, first.TelegramId);
        Assert.Equal("5.00", first.Amount);
        Assert.Equal("USDT", first.Asset);
        Assert.Equal("thanks", first.Comment);
        Assert.True(second!.IsDuplicate);
    }

    [Fact]
    public async Task ProcessWebhookAsync_Rejects_Bad_Signature()
    {
        var sut = CreateSut(Mock.Of<ICryptoPayApiClient>());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ProcessWebhookAsync("{}", "deadbeef", CancellationToken.None));
    }

    [Fact]
    public void TryParseAmountCallback_Reads_Usd_Amount()
    {
        Assert.True(CryptoPayDonationService.TryParseAmountCallback("donate:cb:10", out var amount));
        Assert.Equal(10m, amount);
        Assert.True(CryptoPayDonationService.TryParseConfirmCallback("donate:ok:10", out var confirmed));
        Assert.Equal(10m, confirmed);
        Assert.Equal("donate:ok:10", CryptoPayDonationService.ConfirmCallback(10m));
        Assert.False(CryptoPayDonationService.TryParseAmountCallback("donate", out _));
        Assert.False(CryptoPayDonationService.TryParseAmountCallback("donate:stars:50", out _));
        Assert.False(CryptoPayDonationService.TryParseAmountCallback("donate:ok:10", out _));
        Assert.False(CryptoPayDonationService.TryParseConfirmCallback("donate:cb:10", out _));
        Assert.False(CryptoPayDonationService.TryParseAmountCallback("donate:cb:0", out _));
    }

    private static CryptoPayDonationService CreateSut(
        ICryptoPayApiClient api,
        IOptions<CryptoPayConfiguration>? options = null) =>
        new(api, new CryptoPayPaidInvoiceStore(), options ?? ConfiguredOptions(),
            NullLogger<CryptoPayDonationService>.Instance);

    private static IOptions<CryptoPayConfiguration> ConfiguredOptions() =>
        Options.Create(new CryptoPayConfiguration
        {
            Enabled = true,
            ApiToken = "test-token",
            Amounts = "1,5,10",
            SwapTo = "USDT"
        });
}
