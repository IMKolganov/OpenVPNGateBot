using DataGateVPNBot.Configurations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class KestrelListenOptionsHelperTests
{
    [Fact]
    public void GetListenPort_Returns_5050_When_No_Env_And_No_Config()
    {
        var config = new ConfigurationBuilder().Build();
        var port = KestrelListenOptionsHelper.GetListenPort(config);
        Assert.Equal(5050, port);
    }

    [Theory]
    [InlineData("8080", 8080)]
    [InlineData("443", 443)]
    public void GetListenPort_Returns_Env_Value_When_Valid(string envPort, int expected)
    {
        var config = new ConfigurationBuilder().Build();
        try
        {
            Environment.SetEnvironmentVariable("TELEGRAMBOT_LISTEN_PORT", envPort);
            var port = KestrelListenOptionsHelper.GetListenPort(config);
            Assert.Equal(expected, port);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TELEGRAMBOT_LISTEN_PORT", null);
        }
    }

    [Fact]
    public void GetUseCertificate_Returns_False_When_No_Env_And_Config_False()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BotConfiguration:UseCertificate"] = "false" })
            .Build();
        var useCert = KestrelListenOptionsHelper.GetUseCertificate(config);
        Assert.False(useCert);
    }

    [Fact]
    public void GetUseCertificate_Returns_True_When_Env_True()
    {
        var config = new ConfigurationBuilder().Build();
        try
        {
            Environment.SetEnvironmentVariable("USE_CERTIFICATE", "true");
            var useCert = KestrelListenOptionsHelper.GetUseCertificate(config);
            Assert.True(useCert);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USE_CERTIFICATE", null);
        }
    }
}
