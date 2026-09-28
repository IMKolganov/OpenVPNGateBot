using DataGateVPNBot.Services;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotUser.Responses.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class AdminRecipientServiceTests : IDisposable
{
    private readonly string _cachePath;
    private readonly string? _previousCachePath;
    private readonly string? _previousFallbackIds;

    public AdminRecipientServiceTests()
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
    public async Task GetAdminTelegramIdsAsync_Uses_Cache_When_Dashboard_Refresh_Fails()
    {
        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        store.Update([4242]);

        var userService = new Mock<ITelegramBotUserService>();
        userService.Setup(s => s.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Security.Authentication.AuthenticationException("no token"));

        var services = new ServiceCollection();
        services.AddScoped(_ => userService.Object);
        var provider = services.BuildServiceProvider();

        var sut = new AdminRecipientService(
            provider,
            store,
            Mock.Of<ILogger<AdminRecipientService>>());

        var ids = await sut.GetAdminTelegramIdsAsync(tryRefresh: true);

        Assert.Equal([4242], ids);
    }

    [Fact]
    public async Task GetAdminTelegramIdsAsync_Refreshes_Cache_When_Dashboard_Succeeds()
    {
        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        store.Update([111]);

        var userService = new Mock<ITelegramBotUserService>();
        userService.Setup(s => s.GetAdminsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAdminsResponse
            {
                TelegramBotAdmins =
                [
                    new TelegramBotUserDto { TelegramId = 222 }
                ]
            });

        var services = new ServiceCollection();
        services.AddScoped(_ => userService.Object);
        var provider = services.BuildServiceProvider();

        var sut = new AdminRecipientService(
            provider,
            store,
            Mock.Of<ILogger<AdminRecipientService>>());

        var ids = await sut.GetAdminTelegramIdsAsync(tryRefresh: true);

        Assert.Contains(222, ids);
        Assert.DoesNotContain(111, ids);
    }
}
