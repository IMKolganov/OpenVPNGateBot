using DataGateVPNBot.Services.LetsEncrypt;
using Xunit;

namespace DataGateVPNBot.Tests;

public class AcmeChallengeStoreTests
{
    [Fact]
    public void Add_ThenTryGet_ReturnsValue()
    {
        var token = "unique-token-" + Guid.NewGuid().ToString("N");
        var keyAuth = "challenge-key-auth";
        AcmeChallengeStore.Add(token, keyAuth);
        var found = AcmeChallengeStore.TryGet(token, out var value);
        Assert.True(found);
        Assert.Equal(keyAuth, value);
    }

    [Fact]
    public void TryGet_UnknownToken_ReturnsFalse()
    {
        var found = AcmeChallengeStore.TryGet("nonexistent-token-" + Guid.NewGuid(), out var value);
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void Add_OverwritesExisting()
    {
        var token = "overwrite-token-" + Guid.NewGuid().ToString("N");
        AcmeChallengeStore.Add(token, "first");
        AcmeChallengeStore.Add(token, "second");
        AcmeChallengeStore.TryGet(token, out var value);
        Assert.Equal("second", value);
    }
}
