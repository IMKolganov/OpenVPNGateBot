using DataGateVPNBot.Models.Configurations;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class DonationFeatureFlagsTests
{
    [Fact]
    public void Defaults_Disable_Both_Donation_Channels()
    {
        var stars = new TelegramStarsConfiguration();
        var crypto = new CryptoPayConfiguration();

        Assert.False(stars.Enabled);
        Assert.False(crypto.Enabled);
        Assert.False(stars.IsConfigured);
        Assert.False(crypto.IsConfigured);
        Assert.False(DonationFeatureFlags.IsStarsLive(stars));
        Assert.False(DonationFeatureFlags.IsCryptoPayLive(crypto));
        Assert.False(DonationFeatureFlags.IsAnyDonationChannelLive(stars, crypto));
    }

    [Fact]
    public void Stars_Live_Only_When_Enabled()
    {
        Assert.True(DonationFeatureFlags.IsStarsLive(new TelegramStarsConfiguration { Enabled = true }));
        Assert.False(DonationFeatureFlags.IsStarsLive(new TelegramStarsConfiguration { Enabled = false }));
    }

    [Fact]
    public void CryptoPay_Live_Requires_Enabled_And_Token()
    {
        Assert.False(DonationFeatureFlags.IsCryptoPayLive(new CryptoPayConfiguration
        {
            Enabled = true,
            ApiToken = ""
        }));
        Assert.False(DonationFeatureFlags.IsCryptoPayLive(new CryptoPayConfiguration
        {
            Enabled = false,
            ApiToken = "token"
        }));
        Assert.True(DonationFeatureFlags.IsCryptoPayLive(new CryptoPayConfiguration
        {
            Enabled = true,
            ApiToken = "token"
        }));
    }

    [Fact]
    public void AnyChannel_True_When_Either_Channel_Is_Live()
    {
        var starsOff = new TelegramStarsConfiguration();
        var cryptoOff = new CryptoPayConfiguration();
        var starsOn = new TelegramStarsConfiguration { Enabled = true };
        var cryptoOn = new CryptoPayConfiguration { Enabled = true, ApiToken = "x" };

        Assert.True(DonationFeatureFlags.IsAnyDonationChannelLive(starsOn, cryptoOff));
        Assert.True(DonationFeatureFlags.IsAnyDonationChannelLive(starsOff, cryptoOn));
        Assert.True(DonationFeatureFlags.IsAnyDonationChannelLive(starsOn, cryptoOn));
        Assert.False(DonationFeatureFlags.IsAnyDonationChannelLive(starsOff, cryptoOff));
    }
}
