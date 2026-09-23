using System.Text;
using DataGateVPNBot.Controllers;
using DataGateVPNBot.Services.CryptoPay;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Xunit;

namespace DataGateVPNBot.Tests.Controllers;

public class CryptoPayWebhookControllerTests
{
    [Fact]
    public async Task Webhook_Returns_Unauthorized_When_Service_Rejects_Signature()
    {
        var donations = new Mock<ICryptoPayDonationService>();
        donations.Setup(s => s.ProcessWebhookAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("bad sig"));

        var result = await InvokeAsync(donations.Object, "{}");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Webhook_Thanks_Donor_And_Notifies_Admins_On_New_Payment()
    {
        var donations = new Mock<ICryptoPayDonationService>();
        donations.Setup(s => s.ProcessWebhookAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoPayPaidDonation
            {
                InvoiceId = 9,
                TelegramId = 42,
                Amount = "5",
                Asset = "USDT"
            });

        var bot = new Mock<ITelegramBotClient>();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(s => s.GetTextForTelegramUser(It.IsAny<GetTextForTelegramUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTextForTelegramUserResponse { Text = "Thanks {amount} {asset}" });
        var errors = new Mock<IErrorService>();

        var controller = new CryptoPayWebhookController(
            donations.Object,
            bot.Object,
            localization.Object,
            errors.Object,
            NullLogger<CryptoPayWebhookController>.Instance)
        {
            ControllerContext = BodyContext("{}", "sig")
        };

        var result = await controller.Webhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        errors.Verify(e => e.SendMessageToAdminsAsync(It.Is<string>(m => m.Contains("USDT")), It.IsAny<CancellationToken>()), Times.Once);
        localization.Verify(s => s.GetTextForTelegramUser(
            It.Is<GetTextForTelegramUserRequest>(r => r.TelegramId == 42 && r.Key == "DonateThanks"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_Skips_Thank_You_On_Duplicate()
    {
        var donations = new Mock<ICryptoPayDonationService>();
        donations.Setup(s => s.ProcessWebhookAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoPayPaidDonation { InvoiceId = 9, IsDuplicate = true, TelegramId = 42 });

        var bot = new Mock<ITelegramBotClient>();
        var errors = new Mock<IErrorService>();
        var controller = new CryptoPayWebhookController(
            donations.Object,
            bot.Object,
            Mock.Of<ILocalizationService>(),
            errors.Object,
            NullLogger<CryptoPayWebhookController>.Instance)
        {
            ControllerContext = BodyContext("{}", "sig")
        };

        var result = await controller.Webhook(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        errors.Verify(e => e.SendMessageToAdminsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        bot.VerifyNoOtherCalls();
    }

    private static async Task<IActionResult> InvokeAsync(ICryptoPayDonationService donations, string body)
    {
        var controller = new CryptoPayWebhookController(
            donations,
            Mock.Of<ITelegramBotClient>(),
            Mock.Of<ILocalizationService>(),
            Mock.Of<IErrorService>(),
            NullLogger<CryptoPayWebhookController>.Instance)
        {
            ControllerContext = BodyContext(body, "sig")
        };
        return await controller.Webhook(CancellationToken.None);
    }

    private static ControllerContext BodyContext(string body, string signature)
    {
        var http = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);
        http.Request.Body = new MemoryStream(bytes);
        http.Request.Headers[CryptoPayWebhookSignature.HeaderName] = signature;
        return new ControllerContext { HttpContext = http };
    }
}
