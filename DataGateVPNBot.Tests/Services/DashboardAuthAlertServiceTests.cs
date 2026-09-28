using DataGateVPNBot.Services;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class DashboardAuthAlertServiceTests : IDisposable
{
    private readonly string _cachePath;
    private readonly string? _previousCachePath;
    private readonly string? _previousFallbackIds;

    public DashboardAuthAlertServiceTests()
    {
        _cachePath = Path.Combine(Path.GetTempPath(), $"admin-cache-{Guid.NewGuid():N}.json");
        _previousCachePath = Environment.GetEnvironmentVariable("ADMIN_CACHE_PATH");
        _previousFallbackIds = Environment.GetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS");
        Environment.SetEnvironmentVariable("ADMIN_CACHE_PATH", _cachePath);
        Environment.SetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ADMIN_CACHE_PATH", _previousCachePath);
        Environment.SetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS", _previousFallbackIds);
        if (File.Exists(_cachePath))
            File.Delete(_cachePath);
    }

    [Fact]
    public async Task TryNotifyAuthFailureAsync_Sends_Message_To_Admin_Recipients()
    {
        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        store.Update([4242]);

        var bot = new Mock<ITelegramBotClient>();
        bot.Setup(b => b.SendRequest(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Telegram.Bot.Types.Message());

        var sut = new DashboardAuthAlertService(
            store,
            bot.Object,
            Mock.Of<ILogger<DashboardAuthAlertService>>());

        await sut.TryNotifyAuthFailureAsync("Invalid credentials");

        bot.Verify(
            b => b.SendRequest(
                It.Is<SendMessageRequest>(r =>
                    r.ChatId.Identifier == 4242 &&
                    r.ParseMode == ParseMode.Markdown &&
                    r.Text.Contains("Dashboard API authentication failed") &&
                    r.Text.Contains("Invalid credentials")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryNotifyAuthFailureAsync_Suppresses_Second_Alert_Within_Cooldown()
    {
        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        store.Update([111]);

        var bot = new Mock<ITelegramBotClient>();
        bot.Setup(b => b.SendRequest(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Telegram.Bot.Types.Message());

        var sut = new DashboardAuthAlertService(
            store,
            bot.Object,
            Mock.Of<ILogger<DashboardAuthAlertService>>());

        await sut.TryNotifyAuthFailureAsync("first failure");
        await sut.TryNotifyAuthFailureAsync("second failure");

        bot.Verify(
            b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryNotifyAuthFailureAsync_Sends_Nothing_When_No_Recipients()
    {
        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        var bot = new Mock<ITelegramBotClient>();

        var sut = new DashboardAuthAlertService(
            store,
            bot.Object,
            Mock.Of<ILogger<DashboardAuthAlertService>>());

        await sut.TryNotifyAuthFailureAsync("Invalid credentials");

        bot.Verify(
            b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
