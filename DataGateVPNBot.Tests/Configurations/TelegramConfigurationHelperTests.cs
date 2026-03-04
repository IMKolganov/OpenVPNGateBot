using DataGateVPNBot.Configurations;
using DataGateVPNBot.Models.Configurations;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class TelegramConfigurationHelperTests
{
    [Fact]
    public void ApplyEnvAndValidate_Normalizes_HostAddress()
    {
        var config = new BotConfiguration
        {
            BotToken = "token",
            HostAddress = "https://tgbot.example.com/"
        };
        TelegramConfigurationHelper.ApplyEnvAndValidate(config);
        Assert.Equal("tgbot.example.com", config.HostAddress);
    }

    [Fact]
    public void ApplyEnvAndValidate_Throws_When_BotToken_Missing()
    {
        var config = new BotConfiguration { HostAddress = "host.com" };
        var ex = Assert.Throws<NullReferenceException>(() => TelegramConfigurationHelper.ApplyEnvAndValidate(config));
        Assert.Contains("BotToken", ex.Message);
    }

    [Fact]
    public void ApplyEnvAndValidate_Throws_When_HostAddress_Missing_After_Normalize()
    {
        var envHost = Environment.GetEnvironmentVariable("HOST_ADDRESS");
        Environment.SetEnvironmentVariable("HOST_ADDRESS", null);
        try
        {
            // HostAddress only spaces => normalizes to "" => validation throws
            var config = new BotConfiguration { BotToken = "token", HostAddress = "   " };
            var ex = Assert.Throws<NullReferenceException>(() => TelegramConfigurationHelper.ApplyEnvAndValidate(config));
            Assert.Contains("HostAddress", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOST_ADDRESS", envHost);
        }
    }

    [Fact]
    public void ApplyEnvAndValidate_Overrides_With_Env_Vars()
    {
        var config = new BotConfiguration { BotToken = "old", HostAddress = "old.com" };
        try
        {
            Environment.SetEnvironmentVariable("TELEGRAMBOT_BOT_TOKEN", "env-token");
            Environment.SetEnvironmentVariable("HOST_ADDRESS", "env.example.com");
            TelegramConfigurationHelper.ApplyEnvAndValidate(config);
            Assert.Equal("env-token", config.BotToken);
            Assert.Equal("env.example.com", config.HostAddress);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TELEGRAMBOT_BOT_TOKEN", null);
            Environment.SetEnvironmentVariable("HOST_ADDRESS", null);
        }
    }
}
