using DataGateVPNBot.Configurations;
using DataGateVPNBot.Models.Configurations;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class TelegramStarsConfigurationHelperTests
{
    [Fact]
    public void Default_Configuration_Is_Disabled()
    {
        var config = new TelegramStarsConfiguration();
        Assert.False(config.Enabled);
        Assert.False(config.IsConfigured);
    }

    [Fact]
    public void ApplyEnv_Overrides_Enabled_And_Amounts()
    {
        var config = new TelegramStarsConfiguration { Enabled = true, Amounts = "50" };
        try
        {
            Environment.SetEnvironmentVariable("STARS_ENABLED", "false");
            Environment.SetEnvironmentVariable("STARS_AMOUNTS", " 100,250 ");
            TelegramStarsConfigurationHelper.ApplyEnv(config);
            Assert.False(config.Enabled);
            Assert.False(config.IsConfigured);
            Assert.Equal("100,250", config.Amounts);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STARS_ENABLED", null);
            Environment.SetEnvironmentVariable("STARS_AMOUNTS", null);
        }
    }

    [Fact]
    public void ApplyEnv_Can_Enable_Stars()
    {
        var config = new TelegramStarsConfiguration();
        try
        {
            Environment.SetEnvironmentVariable("STARS_ENABLED", "true");
            TelegramStarsConfigurationHelper.ApplyEnv(config);
            Assert.True(config.Enabled);
            Assert.True(config.IsConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("STARS_ENABLED", null);
        }
    }
}

