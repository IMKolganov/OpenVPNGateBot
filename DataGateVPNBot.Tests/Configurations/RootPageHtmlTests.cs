using DataGateVPNBot.Configurations;
using Xunit;

namespace DataGateVPNBot.Tests.Configurations;

public class RootPageHtmlTests
{
    [Fact]
    public void Render_IncludesStartupHistoryTable()
    {
        var runtimeInfo = new ApplicationRuntimeInfo();
        var history = new List<ApplicationStartupRecord>
        {
            new()
            {
                StartedAtUtc = runtimeInfo.StartedAtUtc,
                Version = "1.2.3.33",
                Environment = "Development",
            },
            new()
            {
                StartedAtUtc = runtimeInfo.StartedAtUtc.AddHours(-2),
                Version = "1.2.3.32",
                Environment = "Development",
            },
        };

        var html = RootPageHtml.Render(
            "1.2.3.33",
            "Development",
            runtimeInfo,
            history);

        Assert.Contains("DataGate VPN Bot", html);
        Assert.Contains("Startup history", html);
        Assert.Contains("<th>Started</th><th>Version</th><th>Environment</th>", html);
        Assert.DoesNotContain("Database", html);
        Assert.Contains("1.2.3.32", html);
    }

    [Fact]
    public void FormatUptime_FormatsSecondsMinutesAndDays()
    {
        Assert.Equal("45s", RootPageHtml.FormatUptime(TimeSpan.FromSeconds(45)));
        Assert.Equal("14m 18s", RootPageHtml.FormatUptime(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(18)));
        Assert.Equal("2d 3h 5m", RootPageHtml.FormatUptime(TimeSpan.FromDays(2) + TimeSpan.FromHours(3) + TimeSpan.FromMinutes(5)));
    }
}
