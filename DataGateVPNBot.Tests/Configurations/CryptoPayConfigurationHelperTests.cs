using DataGateVPNBot.Configurations;
using DataGateVPNBot.Models.Configurations;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class CryptoPayConfigurationHelperTests
{
    [Fact]
    public void ParseAmounts_Splits_Sorts_And_Drops_Invalid()
    {
        var amounts = CryptoPayConfigurationHelper.ParseAmounts("10, 1,abc,5,1,0,-2");
        Assert.Equal(new[] { 1m, 5m, 10m }, amounts);
    }

    [Fact]
    public void ParseAmounts_Empty_Returns_Empty()
    {
        Assert.Empty(CryptoPayConfigurationHelper.ParseAmounts(null));
        Assert.Empty(CryptoPayConfigurationHelper.ParseAmounts("  "));
    }

    [Fact]
    public void ApplyEnv_Overrides_Token_And_Testnet()
    {
        var config = new CryptoPayConfiguration { ApiToken = "old", UseTestnet = false };
        try
        {
            Environment.SetEnvironmentVariable("CRYPTOPAY_API_TOKEN", " env-token ");
            Environment.SetEnvironmentVariable("CRYPTOPAY_USE_TESTNET", "true");
            Environment.SetEnvironmentVariable("CRYPTOPAY_ENABLED", "false");
            CryptoPayConfigurationHelper.ApplyEnv(config);
            Assert.Equal("env-token", config.ApiToken);
            Assert.True(config.UseTestnet);
            Assert.False(config.Enabled);
            Assert.False(config.IsConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRYPTOPAY_API_TOKEN", null);
            Environment.SetEnvironmentVariable("CRYPTOPAY_USE_TESTNET", null);
            Environment.SetEnvironmentVariable("CRYPTOPAY_ENABLED", null);
        }
    }

    [Fact]
    public void IsConfigured_Requires_Enabled_And_Token()
    {
        Assert.False(new CryptoPayConfiguration { Enabled = true, ApiToken = "" }.IsConfigured);
        Assert.False(new CryptoPayConfiguration { Enabled = false, ApiToken = "x" }.IsConfigured);
        Assert.True(new CryptoPayConfiguration { Enabled = true, ApiToken = "x" }.IsConfigured);
        Assert.Equal(CryptoPayConfiguration.TestnetApiBaseUrl, new CryptoPayConfiguration { UseTestnet = true }.ApiBaseUrl);
        Assert.Equal(CryptoPayConfiguration.MainnetApiBaseUrl, new CryptoPayConfiguration().ApiBaseUrl);
    }

    [Fact]
    public void Default_Configuration_Is_Disabled()
    {
        var config = new CryptoPayConfiguration();
        Assert.False(config.Enabled);
        Assert.False(config.IsConfigured);
        Assert.Equal(string.Empty, config.ApiToken);
    }

    [Fact]
    public void ApplyEnv_Can_Enable_When_Token_Present()
    {
        var config = new CryptoPayConfiguration();
        try
        {
            Environment.SetEnvironmentVariable("CRYPTOPAY_ENABLED", "true");
            Environment.SetEnvironmentVariable("CRYPTOPAY_API_TOKEN", "live-token");
            CryptoPayConfigurationHelper.ApplyEnv(config);
            Assert.True(config.Enabled);
            Assert.Equal("live-token", config.ApiToken);
            Assert.True(config.IsConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRYPTOPAY_ENABLED", null);
            Environment.SetEnvironmentVariable("CRYPTOPAY_API_TOKEN", null);
        }
    }
}

