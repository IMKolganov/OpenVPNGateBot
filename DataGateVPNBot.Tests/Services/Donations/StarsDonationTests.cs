using DataGateVPNBot.Configurations;
using DataGateVPNBot.Services.Donations;
using Xunit;

namespace DataGateVPNBot.Tests.Services.Donations;

public class StarsDonationTests
{
    [Fact]
    public void Parse_Callback_And_Payload()
    {
        Assert.True(StarsDonation.TryParseCallback("donate:stars:50", out var stars));
        Assert.Equal(50, stars);
        Assert.False(StarsDonation.TryParseCallback("donate:cb:5", out _));

        Assert.True(StarsDonation.TryParsePayload("stars:42:100", out var telegramId, out var paid));
        Assert.Equal(42, telegramId);
        Assert.Equal(100, paid);
        Assert.True(StarsDonation.IsDonatePreCheckout("XTR", "stars:42:100"));
        Assert.False(StarsDonation.IsDonatePreCheckout("USD", "stars:42:100"));
        Assert.False(StarsDonation.IsDonatePreCheckout("XTR", "stars:bad"));
        Assert.False(StarsDonation.TryParsePayload("stars:0:100", out _, out _));
    }

    [Fact]
    public void ParseAmounts_Sorts_Positive_Ints()
    {
        Assert.Equal(new[] { 50, 100, 250 }, TelegramStarsConfigurationHelper.ParseAmounts("100, 50, x, 250, 50"));
    }

    [Fact]
    public void PaidChargeStore_Ignores_Duplicates()
    {
        var store = new StarsPaidChargeStore();
        Assert.True(store.TryMarkProcessed("charge-1"));
        Assert.False(store.TryMarkProcessed("charge-1"));
        Assert.False(store.TryMarkProcessed(""));
        Assert.True(store.TryMarkProcessed("charge-2"));
    }
}
