using DataGateVPNBot.Localization;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateVPNBot.Tests.Localization;

public class DonateTextFallbacksTests
{
    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Russian)]
    [InlineData(Language.Greek)]
    public void Intro_and_thanks_are_non_empty(Language language)
    {
        Assert.Contains("Stars", DonateTextFallbacks.Get("DonateIntro", language));
        Assert.Contains("{amount}", DonateTextFallbacks.Get("DonatePayButton", language));
        Assert.Contains("{asset}", DonateTextFallbacks.Get("DonateThanks", language));
        Assert.False(string.IsNullOrWhiteSpace(DonateTextFallbacks.Get("DonateStarsTitle", language)));
        var risk = DonateTextFallbacks.Get("DonateCryptoRiskBanner", language);
        Assert.Contains("!!!", risk);
        Assert.Contains("P2P", risk);
        Assert.Contains("{amount}", risk);
        Assert.False(string.IsNullOrWhiteSpace(DonateTextFallbacks.Get("DonateCryptoRiskContinue", language)));
        Assert.False(string.IsNullOrWhiteSpace(DonateTextFallbacks.Get("DonateCryptoRiskIgnore", language)));
        Assert.True(DonateTextFallbacks.Get("DonateCryptoRiskContinue", language).Length <= 64);
        Assert.True(DonateTextFallbacks.Get("DonateCryptoRiskIgnore", language).Length <= 64);
    }
}
