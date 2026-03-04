using DataGateVPNBot.Configurations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class DashboardApiConfigurationHelperTests
{
    [Fact]
    public void GetConfig_Returns_Url_From_Configuration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DashboardApi:Url"] = "https://api.example.com",
                ["DashboardApi:ClientId"] = "client",
                ["DashboardApi:ClientSecret"] = "secret"
            })
            .Build();
        var result = DashboardApiConfigurationHelper.GetConfig(config);
        Assert.Equal("https://api.example.com", result.Url);
        Assert.Equal("client", result.ClientId);
        Assert.Equal("secret", result.ClientSecret);
    }

    [Fact]
    public void GetConfig_Prefers_Env_Over_Configuration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DashboardApi:Url"] = "https://config.com" })
            .Build();
        try
        {
            Environment.SetEnvironmentVariable("DASHBOARDAPI_URL", "https://env.com");
            var result = DashboardApiConfigurationHelper.GetConfig(config);
            Assert.Equal("https://env.com", result.Url);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DASHBOARDAPI_URL", null);
        }
    }

    [Fact]
    public void GetConfig_Returns_Empty_Strings_When_Neither_Env_Nor_Config()
    {
        var config = new ConfigurationBuilder().Build();
        var result = DashboardApiConfigurationHelper.GetConfig(config);
        Assert.Equal(string.Empty, result.Url);
        Assert.Equal(string.Empty, result.ClientId);
        Assert.Equal(string.Empty, result.ClientSecret);
    }
}
