using DataGateVPNBot.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataGateVPNBot.Tests.Services;

public class AdminRecipientStoreTests : IDisposable
{
    private readonly string _cachePath;
    private readonly string? _previousCachePath;
    private readonly string? _previousFallbackIds;

    public AdminRecipientStoreTests()
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
    public void Update_Persists_And_Reloads_Ids()
    {
        var writer = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        writer.Update([111, 222]);

        var reader = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());

        Assert.Equal([111, 222], reader.ResolveRecipients());
    }

    [Fact]
    public void ResolveRecipients_Prefers_Cache_Over_Fallback()
    {
        Environment.SetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS", "333,444");

        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());
        store.Update([111]);

        Assert.Equal([111], store.ResolveRecipients());
        Assert.False(store.IsUsingFallbackReserve);
    }

    [Fact]
    public void ResolveRecipients_Uses_Fallback_Only_When_Cache_Empty()
    {
        Environment.SetEnvironmentVariable("TELEGRAMBOT_FALLBACK_ADMIN_IDS", "333,444");

        var store = new AdminRecipientStore(Mock.Of<ILogger<AdminRecipientStore>>());

        Assert.Equal([333, 444], store.ResolveRecipients());
        Assert.True(store.IsUsingFallbackReserve);
    }
}
